using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace Silly_Things.Codes.PortalGun
{
    public class PortalGun : PhysicsProp
    {
        private AudioSource audio;

        private static NetworkObject portalA;
        private static NetworkObject portalB;
        private bool shootPortalA = true;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            audio = transform.Find("Audio").GetComponent<AudioSource>();
        }

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);

            if (!right)
                return;

            if (playerHeldBy == null || !playerHeldBy.IsOwner)
                return;

            shootPortalA = !shootPortalA;

            SyncSoundsClientRpc(1);
        }

        public override void EquipItem()
        {
            SetControlTips();
            EnableItemMeshes(enable: true);
            playerHeldBy.equippedUsableItemQE = true;
            isPocketed = false;
            if (!hasBeenHeld)
            {
                hasBeenHeld = true;
                if (!isInShipRoom && !StartOfRound.Instance.inShipPhase && StartOfRound.Instance.currentLevel.spawnEnemiesAndScrap)
                {
                    RoundManager.Instance.valueOfFoundScrapItems += scrapValue;
                }
            }
        }

        public override void SetControlTipsForItem()
        {
            SetControlTips();
        }

        private void SetControlTips()
        {
            string[] allLines = {"Place Portal : [LMB]", "Switch Portal (A/B) : [E]"};

            if (IsOwner)
            {
                HUDManager.Instance.ClearControlTips();
                HUDManager.Instance.ChangeControlTipMultiple(allLines, holdingItem: true, itemProperties);
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (!playerHeldBy.IsOwner)
                return;

            if (!buttonDown)
                return;

            ShootPortalServerRpc(shootPortalA);
        }

        [ServerRpc]
        private void ShootPortalServerRpc(bool isPortalA, ServerRpcParams rpcParams = default)
        {
            if (!IsServer)
                return;

            PlayerControllerB player = GetPlayerFromClient(rpcParams.Receive.SenderClientId);
            if (player == null)
                return;

            if (!TryGetHitPoint(player, out RaycastHit hit))
                return;

            NetworkObject portalNetObj = SpawnPortal(hit, isPortalA);
            if (portalNetObj == null)
                return;

            RegisterPortal(portalNetObj, isPortalA);
            LinkPortalsIfReady();

            SyncSoundsClientRpc(0);
        }

        private PlayerControllerB GetPlayerFromClient(ulong clientId)
        {
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player != null && player.OwnerClientId == clientId)
                    return player;
            }

            Plugin.Logger.LogError("Player not found for clientId: " + clientId);
            return null;
        }

        private bool TryGetHitPoint(PlayerControllerB player, out RaycastHit hit)
        {
            hit = default;

            if (player.gameplayCamera == null)
            {
                Plugin.Logger.LogError("Player camera is null");
                return false;
            }

            Ray ray = new Ray(
                player.gameplayCamera.transform.position,
                player.gameplayCamera.transform.forward
            );

            return Physics.Raycast(ray, out hit, 50f);
        }
        private NetworkObject SpawnPortal(RaycastHit hit, bool isPortalA)
        {
            if (Plugin.Instance.PortalPrefab == null)
                return null;

            Quaternion rotation = CalculatePortalRotation(hit.normal);
            Vector3 spawnPos = hit.point + hit.normal * 0.01f;

            GameObject obj = CreatePortalObject(spawnPos, rotation);
            if (obj == null)
                return null;

            Portal portal = SetupPortalComponent(obj, isPortalA);
            PortalView view = SetupPortalView(obj, portal, isPortalA);
            view.playerCameraTransform = playerHeldBy.gameplayCamera.transform;

            Renderer rend = obj.GetComponentInChildren<Renderer>();
            if (rend == null)
                Plugin.Logger.LogError("Portal prefab missing a child Renderer for the portal screen!");

            view.portalScreenRenderer = rend;


            return obj.GetComponent<NetworkObject>();
        }

        private Quaternion CalculatePortalRotation(Vector3 normal)
        {
            if (Vector3.Dot(normal, Vector3.up) > 0.9f)
                return Quaternion.Euler(0, playerHeldBy.gameplayCamera.transform.eulerAngles.y, 0);
            else if (Vector3.Dot(normal, Vector3.down) > 0.9f)
                return Quaternion.Euler(180, playerHeldBy.gameplayCamera.transform.eulerAngles.y, 0);
            else
            {
                Vector3 forward = Vector3.Cross(Vector3.up, -normal);
                return Quaternion.LookRotation(forward, Vector3.up);
            }
        }

        private GameObject CreatePortalObject(Vector3 position, Quaternion rotation)
        {
            GameObject obj = Instantiate(Plugin.Instance.PortalPrefab, position, rotation);

            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj == null)
                netObj = obj.AddComponent<NetworkObject>();
            netObj.Spawn();

            return obj;
        }

        private Portal SetupPortalComponent(GameObject obj, bool isPortalA)
        {
            Portal portal = obj.GetComponent<Portal>();
            if (portal == null)
                portal = obj.AddComponent<Portal>();
            portal.Setup(isPortalA);
            return portal;
        }

        private PortalView SetupPortalView(GameObject obj, Portal portal, bool isPortalA)
        {
            PortalView view = obj.GetComponent<PortalView>();
            if (view == null)
                view = obj.AddComponent<PortalView>();

            view.linkedPortal = isPortalA && portalB != null ? portalB.GetComponent<Portal>() : !isPortalA && portalA != null ? portalA.GetComponent<Portal>() : null;

            GameObject camObj = new GameObject("PortalCamera");
            camObj.transform.SetParent(obj.transform);
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;
            cam.targetTexture = new RenderTexture(512, 512, 16);
            view.portalCamera = cam;

            Renderer rend = obj.GetComponentInChildren<Renderer>();
            view.portalScreenRenderer = rend;

            return view;
        }

        private void RegisterPortal(NetworkObject netObj, bool isPortalA)
        {
            if (isPortalA)
            {
                if (portalA != null && portalA.IsSpawned)
                    portalA.Despawn(true);

                portalA = netObj;
            }
            else
            {
                if (portalB != null && portalB.IsSpawned)
                    portalB.Despawn(true);

                portalB = netObj;
            }
        }

        private void LinkPortalsIfReady()
        {
            if (portalA == null || portalB == null)
                return;

            if (!portalA.IsSpawned || !portalB.IsSpawned)
                return;

            Portal a = portalA.GetComponent<Portal>();
            Portal b = portalB.GetComponent<Portal>();

            if (a == null || b == null)
            {
                Plugin.Logger.LogError("Portal component missing when linking");
                return;
            }

            a.SetLinkedPortal(portalB.NetworkObjectId);
            b.SetLinkedPortal(portalA.NetworkObjectId);
        }

        [ClientRpc]
        private void SyncSoundsClientRpc(int idSound)
        {
            if (idSound == 0)
                audio?.PlayOneShot(Plugin.Instance.SoundOpenCardboardBox);
            else if (idSound == 1)
                audio?.PlayOneShot(Plugin.Instance.SoundCloseCardboardBox);
        }
    }
}
