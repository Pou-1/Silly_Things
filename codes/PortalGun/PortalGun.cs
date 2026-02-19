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
            Quaternion rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
            Vector3 spawnPos = hit.point + hit.normal * 0.02f;

            GameObject obj = Instantiate(Plugin.Instance.PortalPrefab, spawnPos, rotation);

            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            netObj.Spawn();

            Portal portal = obj.GetComponent<Portal>();
            portal.Setup(isPortalA);
            return netObj;
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
