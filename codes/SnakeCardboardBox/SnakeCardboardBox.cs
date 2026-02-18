using GameNetcodeStuff;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Silly_Things.codes.SnakeCardboardBox
{
    public class SnakeCardboardBox : PhysicsProp
    {
        private readonly SnakeCardboardBoxUi ui = new SnakeCardboardBoxUi();
        public static List<SnakeCardboardBox> Instances { get; set; } = new List<SnakeCardboardBox>();
        public bool PlayerHiddenByBox = false;
        private ulong spawnedBoxId;
        private AudioSource? audio;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Instances.Add(this);
            audio = gameObject.transform.Find("Audio").GetComponent<AudioSource>();

            ui.box = this;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (!buttonDown)
                return;

            if (!playerHeldBy.IsOwner)
                return;

            if (ui.IsOpen)
            {
                PlayerHiddenByBox = false;
                ui.CloseUI();
                RemoveBoxServerRpc();
                SyncSoundsServerRpc(1);
            }
            else
            {
                ui.OpenUI();
                PlayerHiddenByBox = true;
                SpawnBoxOnPlayerServerRpc(playerHeldBy.NetworkObjectId);
                SyncSoundsServerRpc(0);
            }
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            PlayerHiddenByBox = false;
            ui.CloseUI();
            RemoveBoxServerRpc();
        }

        public override void OnDestroy()
        {
            PlayerHiddenByBox = false;
            base.OnDestroy();
            RemoveBoxServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncSoundsServerRpc(int idSound)
        {
            SyncSoundsClientRpc(idSound);
        }

        [ClientRpc]
        public void SyncSoundsClientRpc(int idSound)
        {
            if (idSound == 0)
                audio?.PlayOneShot(Plugin.Instance.SoundOpenCardboardBox);
            else if (idSound == 1)
                audio?.PlayOneShot(Plugin.Instance.SoundCloseCardboardBox);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnBoxOnPlayerServerRpc(ulong playerId)
        {
            GameObject boxInstance = Instantiate(Plugin.Instance.BigCardboardBoxPrefab);

            NetworkObject netObj = boxInstance.GetComponent<NetworkObject>();

            netObj.Spawn(true);

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerNetObj))
            {
                PlayerControllerB player = playerNetObj.GetComponent<PlayerControllerB>();
                if (player != null)
                {
                    Transform parent = player.thisPlayerBody != null ? player.thisPlayerBody : player.transform;

                    netObj.TrySetParent(parent, false);
                    netObj.transform.localPosition = new Vector3(0f, 1.8f, 0f);
                    netObj.transform.localRotation = Quaternion.identity;
                }
            }

            spawnedBoxId = netObj.NetworkObjectId;

            HideBoxForOwnerClientRpc(netObj.NetworkObjectId, playerId);
        }

        [ClientRpc]
        public void HideBoxForOwnerClientRpc(ulong boxId, ulong ownerId)
        {
            if (ownerId != NetworkManager.Singleton.LocalClientId)
                return;

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
                .TryGetValue(boxId, out NetworkObject netObj))
            {
                Renderer[] renderers = netObj.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                    r.enabled = false;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveBoxServerRpc()
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
                .TryGetValue(spawnedBoxId, out NetworkObject netObj))
            {
                netObj.Despawn(true);
            }
        }
    }
}
