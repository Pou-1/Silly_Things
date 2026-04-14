using BepInEx;
using GameNetcodeStuff;
using Newtonsoft.Json;
using Silly_Things.codes.CameraItem;
using Silly_Things.codes.Helper;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Netcode;
using UnityEngine;
namespace Silly_Things.Codes.CameraItem
{
    public class PhotoItem : PhysicsProp
    {
        // _____________SAVE / LOAD_____________ \\
        public static List<PhotoItem> Instances { get; set; } = new List<PhotoItem>();
        private Dictionary<int, List<byte>> receivedPhotos = new Dictionary<int, List<byte>>();
        public NetworkVariable<int> UniqueIdNet = new NetworkVariable<int>();
        private const int MAX_CHUNK_SIZE = 900;

        // _____________UI_____________ \\
        public Renderer? photoRenderer;
        public TextMeshPro? dateText; 
        public TextMeshPro? entityNamesText;
        public GameObject? imgPlaceholderGO;
        private Renderer? frameCube;
        private Renderer? frameCube2;
        private Renderer? frameBase;
        private Renderer? frameRounded;

        // _____________PIN_____________ \\
        private bool isPin = false;
        private Vector3 pinPosition;
        private Quaternion pinRotation;
        public LayerMask pinLayerMask;
        private Collider? col;
        public string[] allowedLayers = new string[]
        {
            "Room",
            "Colliders",
            "MiscLevelGeometry",
            "Terrain",
            "DecalStickableSurface",
            "InteractableObject",
            "PhysicsObject"
        };

        // _____________OVERRIDE_____________ \\
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Instances.Add(this);

            frameCube = transform.Find("frameCube/Cube.006")?.GetComponent<Renderer>();
            frameCube2 = transform.Find("frameCube/Cube.011")?.GetComponent<Renderer>();
            frameBase = transform.Find("frameBase")?.GetComponent<Renderer>();
            frameRounded = transform.Find("frameRounded")?.GetComponent<Renderer>();

            UniqueIdNet.OnValueChanged += OnUniqueIdChanged;

            if (IsServer && UniqueIdNet.Value == 0)
            {
                UniqueIdNet.Value = Random.Range(1, int.MaxValue);
            }

            if (IsClient)
            {
                ApplyFrameVariant();
            }

            pinLayerMask = LayerMask.GetMask(allowedLayers);
            col = GetComponent<Collider>();
            photoRenderer = GetComponentInChildren<Renderer>();
            dateText = transform.Find("Date")?.GetComponent<TextMeshPro>();
            entityNamesText = transform.Find("EntityNames")?.GetComponent<TextMeshPro>();
            imgPlaceholderGO = transform.Find("ImgPlaceholder")?.gameObject;
        }

        private void OnUniqueIdChanged(int oldVal, int newVal)
        {
            ApplyFrameVariant();
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

            if (isPin)
            {
                transform.localPosition = pinPosition;
                transform.localRotation = pinRotation;
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (!buttonDown || playerHeldBy == null || !IsOwner)
                return;

            Camera cam = playerHeldBy.gameplayCamera;

            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 2f, pinLayerMask))
            {
                if (hit.transform == playerHeldBy.transform)
                    return;

                if (hit.transform.GetComponent<PlayerControllerB>() != null)
                    return;

                if (hit.transform.GetComponent<EnemyAI>() != null)
                    return;

                playerHeldBy.DiscardHeldObject(true, null, hit.point);

                NetworkObject netObj = hit.transform.GetComponentInParent<NetworkObject>();
                ulong parentId = netObj != null ? netObj.NetworkObjectId : 0;

                TryPinPhotoServerRpc(hit.point, hit.normal, parentId);
            }
        }

        public override void GrabItem()
        {
            base.GrabItem();
            UnPin();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Instances.Remove(this);
        }

        private void ApplyFrameVariant()
        {
            frameCube?.gameObject.SetActive(false);
            frameCube2?.gameObject.SetActive(false);
            frameBase?.gameObject.SetActive(false);
            frameRounded?.gameObject.SetActive(false);

            int index = UniqueIdNet.Value % 3;
            Plugin.Logger.LogError("ApplyFrameVariant");
            if (index == 0 && frameCube != null && frameCube2 != null)
            {
                frameCube.gameObject.SetActive(true);
                frameCube2.gameObject.SetActive(true);
                ApplyPastelColor(frameCube);
                ApplyPastelColor(frameCube2);
            }
            else if (index == 1 && frameBase != null)
            {
                frameBase.gameObject.SetActive(true);
                ApplyPastelColor(frameBase);
            }
            else if (index == 2 && frameRounded != null)
            {
                frameRounded.gameObject.SetActive(true);
                ApplyPastelColor(frameRounded);
            }
            Plugin.Logger.LogError("ApplyFrameVariant END");
        }

        private void ApplyPastelColor(Renderer renderer)
        {
            if (renderer == null)
                return;

            float hue = (UniqueIdNet.Value * 0.618f) % 1f;
            float seed = UniqueIdNet.Value * 0.123f;

            float saturation = Mathf.Lerp(0.2f, 0.4f, Mathf.Abs(Mathf.Sin(seed)));
            float value = Mathf.Lerp(0.8f, 1f, Mathf.Abs(Mathf.Cos(seed)));

            Color pastel = Color.HSVToRGB(hue, saturation, value);

            Material mat = new Material(renderer.material)
            {
                color = pastel
            };

            renderer.material = mat;
        }

        // _____________PIN_____________ \\
        [ServerRpc]
        private void TryPinPhotoServerRpc(Vector3 hitPoint, Vector3 normal, ulong parentNetId)
        {
            ApplyPinClientRpc(hitPoint, normal, parentNetId);
        }

        [ClientRpc]
        private void ApplyPinClientRpc(Vector3 hitPoint, Vector3 normal, ulong parentNetId)
        {
            if (col == null)
                return;

            Transform? parent;

            if (parentNetId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(parentNetId))
            {
                parent = NetworkManager.Singleton.SpawnManager.SpawnedObjects[parentNetId].transform;
            }
            else
            {
                parent = HitShipFallback();
            }

            Vector3 pos = hitPoint + normal * 0.002f;
            Quaternion rot = Quaternion.LookRotation(-normal);
            rot *= Quaternion.Euler(0, 90f, 0f);

            transform.parent = parent;

            if (transform.parent != null)
            {
                pos = transform.parent.InverseTransformPoint(pos);
            }

            pinPosition = pos;
            pinRotation = rot;
            isPin = true;
        }

        private Transform? HitShipFallback()
        {
            if (StartOfRound.Instance != null && StartOfRound.Instance.shipBounds != null)
                return StartOfRound.Instance.shipBounds.transform;

            return null;
        }

        private void UnPin()
        {
            transform.parent = null;
            isPin = false;
        }

        public void SetPhoto(Texture2D texture, string date = "", string entityNames = "")
        {
            if (photoRenderer != null)
            {
                imgPlaceholderGO?.SetActive(false);

                Material mat = new Material(photoRenderer.material)
                {
                    mainTexture = texture
                };

                photoRenderer.material = mat;
            }
            if (dateText != null)
                dateText.text = date;
            if (entityNamesText != null)
                entityNamesText.text = entityNames;
        }

        // _____________MONSTER SCORE_____________ \\
        public static float GetMonsterScore(string monsterName)
        {
            Helper.LogDebugMod("GetMonsterScore", "");
            string lowerMonsterName = monsterName.ToLower();
            float? monsterValue = GetScrapFromAdditionalMonster(lowerMonsterName);

            if (monsterValue == null)
            {
                monsterValue = Plugin.SillyThingsConfig.defaultMonsterValue.Value;
                //Plugin.Logger.LogError("UNKNOWN MONSTER! Add " + monsterName + "to the config files to add a specific value! The default value is currently applied");
            }
            Helper.LogDebugMod("You catch: " + monsterName + " with a value of  " + monsterValue, "");

            return monsterValue.Value * Plugin.SillyThingsConfig.monsterValueMultiplier.Value;
        }

        public static float? GetScrapFromAdditionalMonster(string monsterName)
        {
            Helper.LogDebugMod("GetScrapFromAdditionalMonster", monsterName);
            float? value = null;

            foreach (HelperCameraEnemy.MonsterNameValue m in HelperCameraEnemy.additionalMonsterValues)
            {
                if (monsterName == m.Name)
                {
                    value = m.Value;
                }
            }

            return value;
        }

        // _____________SAVE / LOAD PICTURE_____________ \\
        public void SendPicturesHostToClient(ulong clientId, int uniqueId)
        {
            if (!IsHost)
                return;

            string saveName = GameNetworkManager.Instance.currentSaveFileName;
            string folder = Path.Combine(Paths.GameRootPath, "TempSillyThings", saveName);

            string filePath = Path.Combine(folder, uniqueId + ".jpg");

            if (!File.Exists(filePath))
                return;

            byte[] data = File.ReadAllBytes(filePath);

            string basePath = Path.Combine(folder, uniqueId.ToString());
            string metaPath = basePath + ".json";

            string date = "";
            string entityNames = "";

            if (File.Exists(metaPath))
            {
                string json = File.ReadAllText(metaPath);
                PhotoMeta? meta = JsonConvert.DeserializeObject<PhotoMeta>(json);

                if (meta != null)
                {
                    date = meta.date;
                    entityNames = meta.entities;
                }
            }

            StartCoroutine(SendChunksToClientCoroutine(clientId, uniqueId, data, date, entityNames));
        }

        private IEnumerator SendChunksToClientCoroutine(ulong clientId, int uniqId, byte[] data, string date, string entityNames)
        {
            int totalChunks = Mathf.CeilToInt((float)data.Length / MAX_CHUNK_SIZE);

            for (int i = 0; i < totalChunks; i++)
            {
                int start = i * MAX_CHUNK_SIZE;
                int size = Mathf.Min(MAX_CHUNK_SIZE, data.Length - start);

                byte[] chunk = new byte[size];
                System.Array.Copy(data, start, chunk, 0, size);

                var rpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                };

                SendPhotoChunkToClientClientRpc(uniqId, chunk, i, totalChunks, date, entityNames, rpcParams);

                yield return null;
            }
        }

        [ClientRpc]
        private void SendPhotoChunkToClientClientRpc(int uniqId, byte[] chunk, int index, int total, string date, string entityNames, ClientRpcParams rpcParams = default)
        {
            if (NetworkManager.Singleton.IsHost)
                return;

            ReceiveChunk(uniqId, chunk, index, total, date, entityNames);
        }

        private void ReceiveChunk(int uniqId, byte[] chunk, int index, int total, string date, string entityNames)
        {
            if (!receivedPhotos.ContainsKey(uniqId))
                receivedPhotos[uniqId] = new List<byte>();

            receivedPhotos[uniqId].AddRange(chunk);

            if (index == total - 1)
            {
                byte[] fullData = receivedPhotos[uniqId].ToArray();
                receivedPhotos.Remove(uniqId);

                ApplyPhotoToExistingItem(uniqId, fullData, date, entityNames);
            }
        }

        public void ApplyPhotoToExistingItem(int uniqId, byte[] data, string date, string entityNames)
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(data);

            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
            {
                PhotoItem photo = obj.GetComponent<PhotoItem>();

                if (photo != null && photo.UniqueIdNet.Value == uniqId)
                {
                    photo.SetPhoto(tex, date, entityNames);
                    break;
                }
            }
            ApplyFrameVariant();
        }
    }
}