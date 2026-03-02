using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace Silly_Things.codes.MorphingCase
{
    public class MorphingCase : PhysicsProp
    {
        private readonly MorphingCaseUi ui = new MorphingCaseUi();
        private AudioSource? audio;
        public static MorphingCase? Instance
        {
            get; set;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Instance = this;
            ui.morphingCase = this;
            audio = gameObject.transform.Find("Audio").GetComponent<AudioSource>();

            if (ui.cosmeticsManager != null)
            {
                ui.cosmeticsManager.morphingCase = this;
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (!playerHeldBy.IsOwner)
                return;

            if (ui.IsOpen)
                return;

            ui.OpenUI();
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            Plugin.Instance.CosmeticsManager.RestorePreviousCosmetics();
            ui.ForceCloseUI();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangeSuitServerRpc(ulong playerId, int sourceSuitId)
        {
            ChangeSuitClientRpc(playerId, sourceSuitId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncSoundsServerRpc(int idSound)
        {
            SyncSoundsClientRpc(idSound);
        }

        [ClientRpc]
        public void SyncSoundsClientRpc(int idSound)
        {
            if(idSound == 0)
                audio?.PlayOneShot(Plugin.Instance.SoundOpenUI);
            else if (idSound == 1)
                audio?.PlayOneShot(Plugin.Instance.SoundCloseUI);
        }

        [ClientRpc]
        public void ChangeSuitClientRpc(ulong playerId, int sourceSuitId)
        {
            PlayerControllerB target = StartOfRound.Instance.allPlayerScripts[playerId];
            UnlockableSuit.SwitchSuitForPlayer(target, sourceSuitId);
        }
    }
}
