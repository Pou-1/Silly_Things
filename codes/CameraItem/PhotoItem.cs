using GameNetcodeStuff;
using Silly_Things.codes.CameraItem;
using Silly_Things.codes.Helper;
using TMPro;
using Unity.Netcode;
using UnityEngine;
namespace Silly_Things.Codes.CameraItem
{
    public class PhotoItem : PhysicsProp
    {
        // _____________UI_____________ \\
        public Renderer? photoRenderer;
        public TextMeshPro? dateText; 
        public TextMeshPro? entityNamesText;

        // _____________PIN_____________ \\
        private bool isPin = false;
        private Vector3 pinPosition;
        private Quaternion pinRotation;
        public LayerMask pinLayerMask;
        private Collider? col;
        public string[] allowedLayers = new string[] { "Room", "Colliders", "MiscLevelGeometry", "Terrain", "DecalStickableSurface" };

        // _____________OVERRIDE_____________ \\
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            pinLayerMask = LayerMask.GetMask(allowedLayers);
            col = GetComponent<Collider>();
            photoRenderer = GetComponentInChildren<Renderer>();
            dateText = transform.Find("Date")?.GetComponent<TextMeshPro>();
            entityNamesText = transform.Find("EntityNames")?.GetComponent<TextMeshPro>();
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
                TryPinPhotoServerRpc(hit.point, hit.normal, hit.transform.GetComponent<NetworkObject>()?.NetworkObjectId ?? 0);
            }
        }

        public override void GrabItem()
        {
            base.GrabItem();
            UnPin();
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

            Transform parent = null;

            if (parentNetId != 0 &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(parentNetId))
            {
                parent = NetworkManager.Singleton.SpawnManager.SpawnedObjects[parentNetId].transform;
            }

            float offsetFromWall = 0.03f + (col.bounds.size.z / 2f);

            Vector3 pos = hitPoint + normal * offsetFromWall;
            pos -= transform.up * (transform.localScale.y / 2f);

            Quaternion rot = Quaternion.LookRotation(-normal, Vector3.up);
            rot *= Quaternion.Euler(0, 90f, 0f);

            transform.parent = parent;

            if (transform.parent != null)
            {
                pos = transform.parent.InverseTransformPoint(pos);
                rot = Quaternion.Inverse(transform.parent.rotation) * rot;
            }

            pinPosition = pos;
            pinRotation = rot;
            isPin = true;
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
                Material mat = new Material(photoRenderer.material);
                mat.mainTexture = texture;
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
    }
}