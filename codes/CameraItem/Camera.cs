﻿using BepInEx;
using GameNetcodeStuff;
using Newtonsoft.Json.Linq;
using Silly_Things.codes.BountyContract;
using Silly_Things.codes.CameraItem;
using Silly_Things.codes.Helper;
using Steamworks.Ugc;
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
        private AudioSource? audio;
        private Camera? itemCamera;
        private ParticleSystem? clickParticles;
        private static List<EnemyAI> photographedEnemies = new List<EnemyAI>();
        private float lastPhotoTime = -999f;
        private Light? flashLight;
        Material? photoMat;
        private static Camera photoCamera;
        private static RenderTexture photoRenderTexture;

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
            itemCamera = GetComponentInChildren<Camera>(true);
            audio = transform.Find("Audio")?.GetComponent<AudioSource>();
            clickParticles = GetComponentInChildren<ParticleSystem>(true);
            screenRenderer = transform.Find("Quad")?.GetComponent<Renderer>();
            cubeSuccessRenderer = transform.Find("CubeSucess")?.GetComponent<Renderer>();
            flashLight = GetComponentInChildren<Light>(true);
            valueText = transform.Find("Price")?.GetComponent<TextMeshPro>();

            if (valueText != null)
                valueText.text = scrapValue.ToString() + "$";

            SyncScrapValueServerRpc(scrapValue);
            if (IsServer)
            {
                int variant = UnityEngine.Random.Range(0, 4);
                SyncVariantClientRpc(variant);
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
        }

        // _____________RENDER OVERRIDE_____________ \\
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

        // _____________ZOOM OVERRIDE_____________ \\
        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);
            if (playerHeldBy == null || itemCamera == null)
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
                SyncSoundsServerRpc(2);
                StartCoroutine(ResetGearSoundFlag(Plugin.Instance.SoundGear.length));
            }
        }

        // _____________TAKE PICTURE OVERRIDE_____________ \\
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (!IsOwner)
                return;

            if (Time.time - lastPhotoTime < Plugin.SillyThingsConfig.cameraUseCooldown.Value)
                return;

            lastPhotoTime = Time.time;

            if (!buttonDown || playerHeldBy == null || itemCamera == null)
                return;

            SyncSoundsServerRpc(0);
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

                SyncSoundsServerRpc(3);
                TriggerCameraEffects(monsters);
                StartCoroutine(Success(1f, value));
            }

            StartCoroutine(TakePhotoWithFlash());
        }

        public override void EquipItem()
        {
            base.EquipItem();

            if (playerHeldBy == null)
                return;

            playerHeldBy.equippedUsableItemQE = true;
            isPocketed = false;
            currentFov = Plugin.SillyThingsConfig.cameraFov.Value;

            if (valueText != null)
                valueText.text = scrapValue.ToString() + "$";

            if (itemCamera != null)
            {
                itemCamera.enabled = true;
                itemCamera.fieldOfView = currentFov;
                itemCamera.farClipPlane = Plugin.SillyThingsConfig.cameraCameraFarClipping.Value;
                itemCamera.cullingMask = playerHeldBy.gameplayCamera.cullingMask;
            }
        }

        public override void DiscardItem()
        {
            base.DiscardItem();

            if (itemCamera != null)
                itemCamera.enabled = false;
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

            string[] allLines = { "Pick of Mob = $$ : [LMB]", "Zoom : [A]", "Unzoom : [E]" };

            HUDManager.Instance.ClearControlTips();
            HUDManager.Instance.ChangeControlTipMultiple(allLines, holdingItem: true, itemProperties);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            screenTexture?.Release();
        }

        // _____________COROUTINE_____________ \\
        private IEnumerator TakePhotoWithFlash()
        {
            if (itemCamera == null || flashLight == null)
                yield break;

            itemCamera.cullingMask = playerHeldBy.gameplayCamera.cullingMask;
            flashLight.color = Color.white;
            flashLight.intensity = 65f;
            flashLight.range = 65f;
            flashLight.spotAngle = 125f;

            yield return new WaitForSeconds(0.02f);

            List<PlayerControllerB> players = new List<PlayerControllerB>();
            List<EnemyAI> monsters = new List<EnemyAI>();

            var result = HelperCamera.GetVisibleEntities(playerHeldBy, monsters, players, itemCamera);

            string entitiesStr = HelperCamera.GetEntitiesNames(result.Item1, result.Item2);
            SpawnPhotoServerRpc(entitiesStr, itemCamera.transform.position, itemCamera.transform.rotation, currentFov);

            yield return new WaitForSeconds(0.3f);

            clickParticles?.Play();
            flashLight.intensity = Plugin.SillyThingsConfig.flashIntensity.Value;
            flashLight.color = Color.yellow;
            flashLight.range = Plugin.SillyThingsConfig.flashRange.Value;
            flashLight.spotAngle = Plugin.SillyThingsConfig.flashAngle.Value;

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

            if (valueText != null)
                valueText.text = scrapValue.ToString() + "$ + " + valueToAdd.ToString();

            yield return new WaitForSeconds(duration);

            cubeSuccessRenderer.material.color = original;

            if (valueText != null)
                valueText.text = scrapValue.ToString() + "$";

            SyncScrapValueServerRpc(valueToAdd);
        }

        // _____________COLORS_____________ \\
        [ClientRpc]
        private void SyncVariantClientRpc(int variantIndex)
        {
            ApplyVariant(variantIndex);
        }

        private void ApplyVariant(int index)
        {
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

        // _____________SCRAP VALUE_____________ \\
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

            if (valueText != null)
                valueText.text = value.ToString() + "$";
        }

        // _____________CAMERA_____________ \\
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

        private void TriggerCameraEffects(List<EnemyAI> enemys)
        {
            Helper.LogDebugMod("TriggerCameraEffects", "");
            foreach (EnemyAI enemy in enemys)
            {
                foreach (Renderer r in enemy.GetComponentsInChildren<Renderer>())
                {
                    r.material.EnableKeyword("_EMISSION");
                    r.material.SetColor("_EmissionColor", Color.green * 2f);
                }
            }
        }

        // _____________PHOTO_____________ \\
        private void SavePhotoToDisk(Texture2D tex)
        {
            try
            {
                string folder = Path.Combine(Paths.GameRootPath, "CameraPictures");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string date = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string fileName = date + "_" + playerHeldBy.playerUsername + ".png";
                string path = Path.Combine(folder, fileName);

                byte[] png = tex.EncodeToPNG();

                File.WriteAllBytes(path, png);

                Plugin.Logger.LogInfo("Picture saved: " + path);
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogError("Failed to save photo: " + e);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SpawnPhotoServerRpc(string entitiesStr, Vector3 camPos, Quaternion camRot, float fov, ServerRpcParams rpc = default)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(
                p => p.OwnerClientId == rpc.Receive.SenderClientId
            );

            if (player == null || Plugin.Instance.PhotoItemPrefab == null)
                return;

            Item pictureItem = Plugin.Instance.PhotoItemPrefab.GetComponent<GrabbableObject>().itemProperties;

            NetworkReference netRef = Helper.SpawnScrap(pictureItem, player.transform.position + player.transform.forward * 0.5f, 0);

            if (netRef.netObjectRef.TryGet(out NetworkObject netObj))
            {
                RequestPhotoClientRpc(netObj.NetworkObjectId, camPos, camRot, fov, rpc.Receive.SenderClientId, entitiesStr);
            }
        }

        [ClientRpc]
        private void RequestPhotoClientRpc(ulong photoNetId, Vector3 camPos, Quaternion camRot, float fov, ulong photographerId, string entitiesStr)
        {
            if (NetworkManager.Singleton.LocalClientId != photographerId)
                return;

            int width = Plugin.SillyThingsConfig.pictureResolutionWidth.Value;
            int height = Plugin.SillyThingsConfig.pictureResolutionHeight.Value;

            Texture2D fullTex = CapturePhotoFromView(camPos, camRot, fov, width, height);

            SavePhotoToDisk(fullTex);

            Texture2D networkTex = DownscaleTexture(fullTex, 1024, 576);

            byte[] jpg = networkTex.EncodeToJPG(50);

            SendPhotoToServerServerRpc(photoNetId, jpg, entitiesStr);

            Object.Destroy(fullTex);
            Object.Destroy(networkTex);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendPhotoToServerServerRpc(ulong photoNetId, byte[] jpg, string entitiesStr)
        {
            SendPhotoToClientsClientRpc(photoNetId, jpg, entitiesStr);
        }

        [ClientRpc]
        private void SendPhotoToClientsClientRpc(ulong photoNetId, byte[] jpg, string entitiesStr)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(photoNetId))
                return;

            NetworkObject netObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[photoNetId];
            PhotoItem photo = netObj.GetComponent<PhotoItem>();

            if (photo == null)
                return;

            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(jpg);

            string dateStr = System.DateTime.Now.ToString("HH:mm");

            photo.SetPhoto(tex, dateStr, entitiesStr);
        }

        private Texture2D CapturePhotoFromView(Vector3 pos, Quaternion rot, float fov, int width, int height)
        {
            InitializePhotoCamera(width, height, Plugin.SillyThingsConfig.pictureResolutionDepth.Value);

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

        private Texture2D DownscaleTexture(Texture2D source, int width, int height)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height);

            if (photoMat != null)
                Graphics.Blit(source, rt, photoMat);
            else
                Graphics.Blit(source, rt);

            RenderTexture current = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            RenderTexture.active = current;

            RenderTexture.ReleaseTemporary(rt);

            return tex;
        }

        // _____________SOUND_____________ \\
        [ServerRpc]
        private void SyncSoundsServerRpc(int id)
        {
            SyncSoundsClientRpc(id);
        }

        [ClientRpc]
        public void SyncSoundsClientRpc(int idSound)
        {
            if (idSound == 0)
                audio?.PlayOneShot(Plugin.Instance.SoundShutter);
            else if (idSound == 2)
                audio?.PlayOneShot(Plugin.Instance.SoundGear, 0.4f);
            else if (idSound == 3)
                audio?.PlayOneShot(Plugin.Instance.SoundSucess);
        }
    }
}