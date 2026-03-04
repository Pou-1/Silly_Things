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
        private GameObject? boxObject;

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

            if (playerHeldBy.thisController.isGrounded)
            {
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
                    SpawnBoxOnPlayerServerRpc();
                    SyncSoundsServerRpc(0);
                }
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
            Instances.Remove(this);
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
        public void SpawnBoxOnPlayerServerRpc()
        {
            SpawnBoxOnPlayerClientRpc();
        }

        [ClientRpc]
        public void SpawnBoxOnPlayerClientRpc()
        {
            if (playerHeldBy != GameNetworkManager.Instance.localPlayerController)
            {
                boxObject = Instantiate(Plugin.Instance.BigCardboardBoxPrefab, playerHeldBy.transform.position + new Vector3(0f, 0f, 0f), playerHeldBy.transform.rotation, playerHeldBy.transform);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveBoxServerRpc()
        {
            RemoveBoxClientRpc();
        }

        [ClientRpc]
        public void RemoveBoxClientRpc()
        {
            if (boxObject != null)
            {
                Destroy(boxObject);
            }
        }
    }
}
