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
                }
                else
                {
                    ui.OpenUI();
                    PlayerHiddenByBox = true;
                    SpawnBoxOnPlayerServerRpc();
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
            base.OnDestroy();
            PlayerHiddenByBox = false;
            Instances.Remove(this);
            RemoveBoxServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnBoxOnPlayerServerRpc()
        {
            SpawnBoxOnPlayerClientRpc();
        }

        [ClientRpc]
        public void SpawnBoxOnPlayerClientRpc()
        {
            audio?.PlayOneShot(Plugin.Instance.SoundOpenCardboardBox, 0.5f);
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
            audio?.PlayOneShot(Plugin.Instance.SoundCloseCardboardBox, 0.5f);
            if (boxObject != null)
            {
                Destroy(boxObject);
            }
        }
    }
}
