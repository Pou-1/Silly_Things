using BepInEx;
using GameNetcodeStuff;
using Newtonsoft.Json;
using Silly_Things.codes.CameraItem;
using Silly_Things.codes.Helper;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
namespace Silly_Things.Codes.CameraItem
{
    public class PhotoItem : PhysicsProp
    {
        // _____________SAVE / LOAD_____________ \\
        public static List<PhotoItem> Instances { get; set; } = new List<PhotoItem>();
        public NetworkVariable<int> UniqueIdNet = new NetworkVariable<int>();
        private const int MAX_CHUNK_SIZE = 900;

        // _____________UI_____________ \\
        public Renderer? photoRenderer;
        public TextMeshPro? dateText; 
        public TextMeshPro? entityNamesText;
        public GameObject? imgPlaceholderGO;

        // _____________PREVIEW_____________ \\
        /*private GameObject? ghostPreview;
        private Renderer? ghostRenderer;
        private Material? ghostMat;
        */
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

            UniqueIdNet.OnValueChanged += OnUniqueIdChanged;

            if (IsServer && UniqueIdNet.Value == 0)
            {
                UniqueIdNet.Value = Random.Range(1, int.MaxValue);
            }
            else
            {
                UniqueIdNet.Value = UniqueIdNet.Value;
            }

            pinLayerMask = LayerMask.GetMask(allowedLayers);
            col = GetComponent<Collider>();
            photoRenderer = GetComponentInChildren<Renderer>();
            dateText = transform.Find("Date")?.GetComponent<TextMeshPro>();
            entityNamesText = transform.Find("EntityNames")?.GetComponent<TextMeshPro>();
            imgPlaceholderGO = transform.Find("ImgPlaceholder")?.gameObject;

            /*ghostPreview = Instantiate(this.gameObject);
            Destroy(ghostPreview.GetComponent<PhotoItem>());
            Destroy(ghostPreview.GetComponent<NetworkObject>());
            Destroy(ghostPreview.GetComponent<Rigidbody>());
            Destroy(ghostPreview.GetComponent<Collider>());
            ghostRenderer = ghostPreview.GetComponent<Renderer>();

            Renderer[] renderers = ghostPreview.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                Material mat = new Material(r.material);
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = 0.3f;
                    mat.color = c;
                }
                mat.SetInt("_ZWrite", 0);
                mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                r.material = mat;
            }

            ghostPreview.transform.localScale = transform.localScale;
            ghostPreview.layer = 0;
            ghostPreview.SetActive(false);*/
        }

        private void OnUniqueIdChanged(int oldVal, int newVal)
        {
            UniqueIdNet.Value = newVal;
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

            /*if (playerHeldBy != null && !isPocketed && IsOwner && !isPin && ghostPreview != null && ghostMat != null)
            {
                Camera cam = playerHeldBy.gameplayCamera;

                if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 2f, pinLayerMask))
                {
                    if (hit.transform.GetComponent<PlayerControllerB>() != null)
                        return;

                    if (hit.transform.GetComponent<EnemyAI>() != null)
                        return;

                    float offset = 0.01f;
                    Vector3 pos = hit.point + hit.normal * offset;
                    Quaternion rot = Quaternion.LookRotation(-hit.normal);
                    rot *= Quaternion.Euler(0, 90f, 0f);

                    ghostPreview.transform.position = pos;
                    ghostPreview.transform.rotation = rot;
                    ghostPreview.transform.localScale = Vector3.one * 0.05f;
                    ghostMat.color = new Color(0f, 1f, 0f, 0.3f);
                    ghostPreview.SetActive(true);
                }
                else
                {
                    ghostPreview.SetActive(false);
                }
            }
            else
            {
                ghostPreview?.SetActive(false);
            }*/
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

            //ghostPreview?.SetActive(false);

            UnPin();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Instances.Remove(this);
        }

        // _____________OTHER_____________ \\
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

                mat.SetFloat("_VignetteIntensity", 0.3f);
                mat.SetFloat("_BlurStrength", 0.5f);
                photoRenderer.material = mat;
            }
            if (dateText != null)
                dateText.text = date;
            if (entityNamesText != null)
                entityNamesText.text = entityNames;
        }

        public static float GetMonsterScore(string monsterName)
        {
            Helper.LogDebugMod("GetMonsterScore", "");
            string lowerMonsterName = monsterName.ToLower();
            float? monsterValue = GetScrapFromAdditionalMonster(lowerMonsterName);

            if (monsterValue == null)
            {
                monsterValue = Plugin.SillyThingsConfig.defaultMonsterValue.Value;
                Plugin.Logger.LogError("UNKNOWN MONSTER! Add " + monsterName + "to the config files to add a specific value! The default value is currently applied");
            }
            Helper.LogDebugMod("You catch: " + monsterName + " with a value of  " + monsterValue, "");

            return monsterValue.Value * Plugin.SillyThingsConfig.monsterValueMultiplier.Value;
        }

        public static float? GetScrapFromAdditionalMonster(string monsterName)
        {
            Helper.LogDebugMod("GetScrapFromAdditionalMonster", monsterName);
            float? value = null;

            foreach (HelperCamera.MonsterNameValue m in HelperCamera.additionalMonsterValues)
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
            string folder = Path.Combine(Paths.GameRootPath, "TempPhotos", saveName);

            string filePath = Path.Combine(folder, uniqueId + ".png");

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

                date = meta.date;
                entityNames = meta.entities;
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

                SendPhotoChunkToClientClientRpc(clientId, uniqId, chunk, i, totalChunks, date, entityNames);

                yield return null;
            }
        }

        [ClientRpc]
        private void SendPhotoChunkToClientClientRpc(ulong targetClientId, int uniqId, byte[] chunk, int index, int total, string date, string entityNames, ClientRpcParams rpcParams = default)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId)
                return;

            ReceiveChunk(uniqId, chunk, index, total, date, entityNames);
        }

        private Dictionary<string, List<byte>> receivedPhotos = new Dictionary<string, List<byte>>();

        private void ReceiveChunk(int uniqId, byte[] chunk, int index, int total, string date, string entityNames)
        {
            if (!receivedPhotos.ContainsKey(uniqId.ToString()))
                receivedPhotos[uniqId.ToString()] = new List<byte>();

            receivedPhotos[uniqId.ToString()].AddRange(chunk);

            if (index == total - 1)
            {
                byte[] fullData = receivedPhotos[uniqId.ToString()].ToArray();
                receivedPhotos.Remove(uniqId.ToString());

                ApplyPhotoToExistingItem(uniqId, fullData, date, entityNames);
            }
        }

        public void ApplyPhotoToExistingItem(int uniqId, byte[] data, string date, string entityNames)
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(data);

            Plugin.Logger.LogError("aaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            Plugin.Logger.LogError("aaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            Plugin.Logger.LogError("aaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            Plugin.Logger.LogError("aaaaaaaaaaaaaaaaaaaaaaaaaaaa");

            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
            {
                PhotoItem photo = obj.GetComponent<PhotoItem>();

                if (photo != null && photo.UniqueIdNet.Value == uniqId)
                {
                    photo.SetPhoto(tex, date, entityNames);
                    break;
                }
            }
        }
    }
}