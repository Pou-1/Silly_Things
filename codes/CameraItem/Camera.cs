﻿using BepInEx;
using GameNetcodeStuff;
using Newtonsoft.Json;
using Silly_Things.codes.BountyContract;
using Silly_Things.codes.CameraItem;
using Silly_Things.codes.Helper;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Silly_Things.Codes.CameraItem
{
    public class CameraItem : PhysicsProp
    {
        private float lastPhotoTime = -999f;
        private const int MAX_CHUNK_SIZE = 900;
        private List<EnemyAI> photographedEnemies = new List<EnemyAI>();
        public NetworkVariable<int> UniqueIdNet = new NetworkVariable<int>();
        private Dictionary<ulong, List<byte>> photoChunks = new Dictionary<ulong, List<byte>>();

        // _____________OBJECT_____________ \\
        private static RenderTexture? photoRenderTexture;
        private static Camera? photoCamera;
        private Light? flashLight;
        private ParticleSystem? clickParticles;
        private Camera? itemCamera;
        private Material? photoMat;
        private AudioSource? audio;

        // _____________BATTERY_____________ \\
        public bool HasBattery => !itemProperties.requiresBattery || (insertedBattery != null && insertedBattery.charge > 0.01f);
        private float batteryUsagePerShot;

        // _____________UI_____________ \\
        private Renderer? screenRenderer;
        private Renderer? cubeSuccessRenderer;
        public TextMeshPro? valueText;
        private RenderTexture? screenTexture;
        private float lastScreenUpdate;

        // _____________ZOOM_____________ \\
        private float zoomFov = 20f;
        private float zoomStep = 5f;
        private bool isGearSoundPlaying = false;
        private float currentFov;

        // _____________OVERRIDE_____________ \\
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            photographedEnemies.Clear();
            SyncScrapValueServerRpc(0);

            UniqueIdNet.OnValueChanged += OnUniqueIdChanged;

            if (IsServer && UniqueIdNet.Value == 0)
            {
                UniqueIdNet.Value = Random.Range(1, 4);
            }
            else
            {
                UniqueIdNet.Value = UniqueIdNet.Value;
            }

            flashLight = GetComponentInChildren<Light>(true);
            itemCamera = GetComponentInChildren<Camera>(true);
            clickParticles = GetComponentInChildren<ParticleSystem>(true);
            audio = transform.Find("Audio")?.GetComponent<AudioSource>();
            valueText = transform.Find("Price")?.GetComponent<TextMeshPro>();
            screenRenderer = transform.Find("Quad")?.GetComponent<Renderer>();
            cubeSuccessRenderer = transform.Find("CubeSucess")?.GetComponent<Renderer>();

            if (IsServer)
            {
                SyncVariantClientRpc();
            }

            if (Plugin.SillyThingsConfig.cameraCanUpdateScreen.Value && itemCamera != null && screenRenderer != null)
            {
                screenTexture = new RenderTexture(
                    Plugin.SillyThingsConfig.screenResolutionWidth.Value,
                    Plugin.SillyThingsConfig.screenResolutionHeight.Value,
                    Plugin.SillyThingsConfig.screenResolutionDepth.Value
                );

                itemCamera.targetTexture = screenTexture;

                if (photoMat != null)
                {
                    screenRenderer.material = new Material(photoMat);
                    screenRenderer.material.mainTexture = screenTexture;
                }
                else
                {
                    screenRenderer.material = new Material(screenRenderer.material);
                    screenRenderer.material.mainTexture = screenTexture;
                }
            }

            if (itemCamera != null)
                itemCamera.enabled = false;

            if (Plugin.SillyThingsConfig.cameraHasBattery.Value)
            {
                itemProperties.requiresBattery = true;

                if (insertedBattery == null)
                    insertedBattery = new Battery(false, 1f);

                insertedBattery.charge = 1f;

                int maxShots = Mathf.Max(1, Plugin.SillyThingsConfig.cameraBatteryNumberOfPickBeforeZero.Value);
                batteryUsagePerShot = 1f / maxShots;
            }
            else
            {
                itemProperties.requiresBattery = false;
            }

            UpdateUI();
        }

        public override int GetItemDataToSave()
        {
            return UniqueIdNet.Value;
        }

        public override void LoadItemSaveData(int saveData)
        {
            UniqueIdNet.Value = saveData;
        }

        public override void LateUpdate()
        {
            base.LateUpdate();

            if (!Plugin.SillyThingsConfig.cameraCanUpdateScreen.Value || !isHeld)
                return;

            if (itemCamera == null)
                return;

            if (Time.time - lastScreenUpdate < Plugin.SillyThingsConfig.cameraScreenUpdateRate.Value)
                return;

            lastScreenUpdate = Time.time;
            itemCamera.Render();
        }

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);

            if (playerHeldBy == null || itemCamera == null || !UseBatteryAndHasBattery())
                return;

            float oldFov = currentFov;

            currentFov += right ? -zoomStep : zoomStep;
            currentFov = Mathf.Clamp(currentFov, zoomFov, Plugin.SillyThingsConfig.cameraFov.Value);

            if (Mathf.Approximately(oldFov, currentFov))
                return;

            itemCamera.fieldOfView = currentFov;

            if (!isGearSoundPlaying)
            {
                isGearSoundPlaying = true;
                PlayFxServerRpc(1);
                StartCoroutine(ResetGearSoundFlag(Plugin.Instance.SoundGear.length));
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (!IsOwner || !UseBatteryAndHasBattery())
                return;

            if (Time.time - lastPhotoTime < Plugin.SillyThingsConfig.cameraUseCooldown.Value)
                return;

            lastPhotoTime = Time.time;

            if (!buttonDown || playerHeldBy == null || itemCamera == null)
                return;

            PlayFxServerRpc(0);
            List<PlayerControllerB> players = new List<PlayerControllerB>();
            List<EnemyAI> monsters = new List<EnemyAI>();
            (monsters, _) = HelperCamera.GetVisibleEntities(playerHeldBy, monsters, players, itemCamera);
            int value = (int)HelperCamera.GetMonstersScore(monsters, photographedEnemies);

            if (value > 0)
            {
                foreach (EnemyAI enemy in monsters)
                {
                    if (!photographedEnemies.Contains(enemy))
                        photographedEnemies.Add(enemy);
                }

                PlayFxServerRpc(2);
                StartCoroutine(Success(1f, value));
            }

            StartCoroutine(TakePhotoWithFlash());
            if (insertedBattery != null)
            {
                insertedBattery.charge = Mathf.Clamp01(insertedBattery.charge - batteryUsagePerShot);
            }
            UpdateUI();
        }

        public override void EquipItem()
        {
            base.EquipItem();

            playerHeldBy.equippedUsableItemQE = true;
            isPocketed = false;
            currentFov = Plugin.SillyThingsConfig.cameraFov.Value;

            UpdateUI();

            if (itemCamera != null)
            {
                itemCamera.enabled = true;
                itemCamera.fieldOfView = currentFov;
                itemCamera.farClipPlane = Plugin.SillyThingsConfig.cameraCameraFarClipping.Value;
                itemCamera.cullingMask = playerHeldBy.gameplayCamera.cullingMask;
            }
        }

        public override void ChargeBatteries()
        {
            UpdateUI();
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            UpdateUI();
        }

        public override void PocketItem()
        {
            base.PocketItem();

            if (itemCamera != null)
                itemCamera.enabled = false;
        }

        public override void SetControlTipsForItem()
        {
            if (!IsOwner)
                return;

            string[] allLines = { "Pic of Mob = $$ : [LMB]", "Zoom : [E]", "Unzoom : [A/Q]" };

            HUDManager.Instance.ClearControlTips();
            HUDManager.Instance.ChangeControlTipMultiple(allLines, holdingItem: true, itemProperties);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            screenTexture?.Release();
        }

        public void UpdateUI()
        {
            if (Plugin.SillyThingsConfig.cameraHasBattery.Value)
            {
                if (screenRenderer != null && valueText != null)
                {
                    if (!HasBattery && Plugin.SillyThingsConfig.cameraCanUpdateScreen.Value)
                        screenRenderer.enabled = false;
                    else if(HasBattery && Plugin.SillyThingsConfig.cameraCanUpdateScreen.Value)
                        screenRenderer.enabled = true;

                    int shotsLeft = Mathf.CeilToInt(insertedBattery.charge / batteryUsagePerShot);
                    valueText.text = scrapValue.ToString() + "$ (" + shotsLeft + ")";
                }
            }
            else
            {
                if (valueText != null)
                    valueText.text = scrapValue.ToString() + "$";
            }

            if (playerHeldBy == null && screenRenderer != null && itemCamera != null)
            {
                screenRenderer.enabled = false;
                itemCamera.enabled = false;
            }
        }

        private void OnUniqueIdChanged(int oldVal, int newVal)
        {
            UniqueIdNet.Value = newVal;
        }

        // _____________COROUTINE_____________ \\
        private IEnumerator BigFlashRoutine()
        {
            yield return new WaitForSeconds(0.3f);

            clickParticles?.Play();

            if (flashLight == null)
                yield break;

            flashLight.intensity = Plugin.SillyThingsConfig.flashIntensity.Value;
            flashLight.range = Plugin.SillyThingsConfig.flashRange.Value;
            flashLight.spotAngle = Plugin.SillyThingsConfig.flashAngle.Value;
            flashLight.color = Color.yellow;
            flashLight.enabled = true;

            yield return new WaitForSeconds(0.1f);

            flashLight.enabled = false;
        }

        private IEnumerator TakePhotoWithFlash()
        {
            if (itemCamera == null || flashLight == null)
                yield break;

            itemCamera.cullingMask = playerHeldBy.gameplayCamera.cullingMask;
            flashLight.color = Color.white;
            flashLight.intensity = 65f;
            flashLight.range = 65f;
            flashLight.spotAngle = 125f;
            flashLight.enabled = true;

            yield return new WaitForSeconds(0.02f);

            List<PlayerControllerB> players = new List<PlayerControllerB>();
            List<EnemyAI> monsters = new List<EnemyAI>();

            var result = HelperCamera.GetVisibleEntities(playerHeldBy, monsters, players, itemCamera);

            string entitiesStr = HelperCamera.GetEntitiesNames(result.Item1, result.Item2);

            int uniqueId = Random.Range(1, int.MaxValue);
            SpawnPhotoServerRpc(entitiesStr, itemCamera.transform.position, itemCamera.transform.rotation, currentFov, uniqueId);

            PlayFxServerRpc(3);
            StartCoroutine(BigFlashRoutine());

            yield return new WaitForSeconds(0.1f);

            flashLight.intensity = 0;
        }

        private IEnumerator ResetGearSoundFlag(float duration)
        {
            Helper.LogDebugMod("ResetGearSoundFlag", duration.ToString());
            yield return new WaitForSeconds(duration);
            isGearSoundPlaying = false;
        }

        private IEnumerator Success(float duration, int valueToAdd)
        {
            Helper.LogDebugMod("Success", "");

            if (cubeSuccessRenderer == null)
                yield break;

            Color original = cubeSuccessRenderer.material.color;
            cubeSuccessRenderer.material.color = Color.green;

            UpdateUI();

            yield return new WaitForSeconds(duration);

            cubeSuccessRenderer.material.color = original;

            UpdateUI();

            SyncScrapValueServerRpc(valueToAdd);
        }

        // _____________RPC_____________ \\
        [ServerRpc]
        private void PlayFxServerRpc(int fxId)
        {
            PlayFxClientRpc(fxId);
        }

        [ClientRpc]
        private void PlayFxClientRpc(int fxId)
        {
            switch (fxId)
            {
                case 0:
                    audio?.PlayOneShot(Plugin.Instance.SoundShutter);
                    break;
                case 1:
                    audio?.PlayOneShot(Plugin.Instance.SoundGear, 0.4f);
                    break;
                case 2:
                    audio?.PlayOneShot(Plugin.Instance.SoundSucess);
                    break;
                case 3:
                    if (!IsOwner)
                        StartCoroutine(BigFlashRoutine());
                    break;
            }
        }

        [ClientRpc]
        private void SyncVariantClientRpc()
        {
            ApplyVariant();
        }

        [ServerRpc]
        private void SyncScrapValueServerRpc(int valueToAdd)
        {
            scrapValue += valueToAdd;
            SyncScrapValueClientRpc(scrapValue);
        }

        [ClientRpc]
        private void SyncScrapValueClientRpc(int value)
        {
            SetScrapValue(value);
            UpdateUI();
        }

        // _____________COLORS_____________ \\
        public bool UseBatteryAndHasBattery()
        {
            if (Plugin.SillyThingsConfig.cameraHasBattery.Value)
            {
                if (HasBattery)
                    return true;
                else
                    return false;
            }
            else
                return true;
        }

        private void ApplyVariant()
        {
            int index = UniqueIdNet.Value;
            Transform gold = transform.Find("CameraGOLD");
            Transform blue = transform.Find("CameraBlue");
            Transform black = transform.Find("CameraBlack");
            Transform pink = transform.Find("CameraPink");

            List<Transform> models = new List<Transform>();

            if (gold != null)
                models.Add(gold);
            if (blue != null)
                models.Add(blue);
            if (black != null)
                models.Add(black);
            if (pink != null)
                models.Add(pink);

            if (models.Count == 0)
                return;

            index = Mathf.Clamp(index, 0, models.Count - 1);

            for (int i = 0; i < models.Count; i++)
                models[i].gameObject.SetActive(i == index);

            if (Plugin.Instance.photoShaderBlack != null && Plugin.Instance.photoShaderBlue != null && Plugin.Instance.photoShaderGold != null && Plugin.Instance.photoShader != null)
            {
                Shader[] shaders =
                {
                    Plugin.Instance.photoShaderGold,
                    Plugin.Instance.photoShaderBlue,
                    Plugin.Instance.photoShaderBlack,
                    Plugin.Instance.photoShader
                };

                if (photoMat != null)
                    Destroy(photoMat);

                photoMat = new Material(shaders[index]);
            }

            Plugin.Logger.LogInfo($"Camera variant synced: {models[index].name}");
        }

        // _____________PHOTO_____________ \\
        private static void InitializePhotoCamera(int width, int height, int depth)
        {
            if (photoCamera != null)
                return;

            GameObject camObj = new GameObject("SillyThingsPhotoCamera");

            Object.DontDestroyOnLoad(camObj);

            photoCamera = camObj.AddComponent<Camera>();
            photoCamera.enabled = false;

            photoRenderTexture = new RenderTexture(width, height, depth);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SpawnPhotoServerRpc(string entitiesStr, Vector3 camPos, Quaternion camRot, float fov, int uniqueId, ServerRpcParams rpc = default)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(
                p => p.OwnerClientId == rpc.Receive.SenderClientId
            );

            if (player == null || Plugin.Instance.PhotoItemPrefab == null)
                return;

            Item pictureItem = Plugin.Instance.PhotoItemPrefab.GetComponent<GrabbableObject>().itemProperties;
            NetworkReference netRef = Helper.SpawnScrap(pictureItem, player.transform.position + player.transform.forward * 0.5f, 5);

            if (netRef.netObjectRef.TryGet(out NetworkObject netObj))
            {
                RequestPhotoClientRpc(netObj.NetworkObjectId, uniqueId, camPos, camRot, fov, rpc.Receive.SenderClientId, entitiesStr);
            }
        }

        [ClientRpc]
        private void RequestPhotoClientRpc(ulong photoNetId, int uniqueId, Vector3 camPos, Quaternion camRot, float fov, ulong photographerId, string entitiesStr)
        {
            if (NetworkManager.Singleton.LocalClientId != photographerId)
                return;

            int width = Plugin.SillyThingsConfig.pictureResolutionWidth.Value;
            int height = Plugin.SillyThingsConfig.pictureResolutionHeight.Value;

            Texture2D? fullTex = CapturePhotoFromView(camPos, camRot, fov, width, height);

            if (fullTex != null)
            {
                HelperCamera.SavePhotoToDisk(fullTex, playerHeldBy.playerUsername);

                if (photoMat != null)
                {
                    //Texture2D networkTex = HelperCamera.DownscaleTexture(fullTex, 640, 360, photoMat);
                    Texture2D networkTex = HelperCamera.DownscaleTexture(fullTex, 1024, 576, photoMat);

                    byte[] jpg = networkTex.EncodeToJPG(75);

                    if (jpg != null)
                    {
                        StartCoroutine(SendChunksCoroutine(photoNetId, uniqueId, jpg, entitiesStr));
                    }

                    Object.Destroy(networkTex);
                }

                Object.Destroy(fullTex);
            }
        }

        private void ApplyPhoto(ulong photoNetId, int uniqueId, byte[] jpg, string entitiesStr)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(photoNetId))
                return;

            NetworkObject netObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[photoNetId];
            PhotoItem photo = netObj.GetComponent<PhotoItem>();
            photo.UniqueIdNet.Value = uniqueId;

            if (photo == null)
                return;

            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(jpg);

            string dateStr = System.DateTime.Now.ToString("HH:mm");

            photo.SetPhoto(tex, dateStr, entitiesStr);

            if (IsHost)
            {
                string saveName = GameNetworkManager.Instance.currentSaveFileName;
                string folder = Path.Combine(Paths.GameRootPath, "TempPhotos", saveName);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string basePath = Path.Combine(folder, uniqueId.ToString());

                HelperCamera.SaveTemp(jpg, uniqueId);

                PhotoMeta meta = new PhotoMeta
                {
                    id = uniqueId,
                    date = dateStr,
                    entities = entitiesStr
                };

                string json = JsonConvert.SerializeObject(meta);
                File.WriteAllText(basePath + ".json", json);
            }
        }

        private Texture2D? CapturePhotoFromView(Vector3 pos, Quaternion rot, float fov, int width, int height)
        {
            InitializePhotoCamera(width, height, Plugin.SillyThingsConfig.pictureResolutionDepth.Value);

            if (photoCamera == null || photoRenderTexture == null)
                return null;

            photoCamera.transform.position = pos;
            photoCamera.transform.rotation = rot;
            photoCamera.fieldOfView = fov;

            var localPlayer = GameNetworkManager.Instance.localPlayerController;

            if (localPlayer != null && localPlayer.gameplayCamera != null)
                photoCamera.cullingMask = localPlayer.gameplayCamera.cullingMask;

            if (photoRenderTexture.width != width || photoRenderTexture.height != height)
            {
                photoRenderTexture.Release();
                photoRenderTexture = new RenderTexture(width, height, Plugin.SillyThingsConfig.pictureResolutionDepth.Value);
            }

            photoCamera.targetTexture = photoRenderTexture;

            photoCamera.Render();

            RenderTexture processed = RenderTexture.GetTemporary(width, height);

            if (photoMat != null)
                Graphics.Blit(photoRenderTexture, processed, photoMat);
            else
                Graphics.Blit(photoRenderTexture, processed);

            RenderTexture current = RenderTexture.active;
            RenderTexture.active = processed;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            RenderTexture.active = current;

            RenderTexture.ReleaseTemporary(processed);
            photoCamera.targetTexture = null;

            return tex;
        }

        // _____________PHOTO CHUNKS_____________ \\
        private IEnumerator SendChunksCoroutine(ulong photoNetId, int uniqueId, byte[] data, string entitiesStr)
        {
            int totalChunks = Mathf.CeilToInt((float)data.Length / MAX_CHUNK_SIZE);

            for (int i = 0; i < totalChunks; i++)
            {
                int start = i * MAX_CHUNK_SIZE;
                int size = Mathf.Min(MAX_CHUNK_SIZE, data.Length - start);

                byte[] chunk = new byte[size];
                System.Array.Copy(data, start, chunk, 0, size);

                SendPhotoChunkServerRpc(photoNetId, uniqueId, chunk, i, totalChunks, entitiesStr);

                yield return new WaitForSeconds(0.001f);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendPhotoChunkServerRpc(ulong photoNetId, int uniqueId, byte[] chunk, int index, int total, string entitiesStr)
        {
            SendPhotoChunkClientRpc(photoNetId, uniqueId, chunk, index, total, entitiesStr);
        }

        [ClientRpc]
        private void SendPhotoChunkClientRpc(ulong photoNetId, int uniqueId, byte[] chunk, int index, int total, string entitiesStr)
        {
            if (!photoChunks.ContainsKey(photoNetId))
                photoChunks[photoNetId] = new List<byte>();

            photoChunks[photoNetId].AddRange(chunk);

            if (index == total - 1)
            {
                byte[] fullData = photoChunks[photoNetId].ToArray();
                photoChunks.Remove(photoNetId);

                ApplyPhoto(photoNetId, uniqueId, fullData, entitiesStr);
            }
        }
    }
}