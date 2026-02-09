using GameNetcodeStuff;
using MoreCompany;
using MoreCompany.Cosmetics;
using System.Collections.Generic;
using Unity.Netcode;

namespace Silly_Things.codes.MorphingCase
{
    internal class ManageCosmetics
    {
        private List<string> previousCosmetics;
        private int previousSuitId = -1;
        private string previousUsername;

        public bool HasStoredCosmetics => previousCosmetics != null;

        public void MorphToPlayer(PlayerControllerB source)
        {
            PlayerControllerB target = StartOfRound.Instance.localPlayerController;
            if (source == null || target == null || !target.isPlayerControlled)
                return;

            if (previousUsername != null)
                return;

            SavePreviousState(target);
            ApplyCosmetics(target, source);
            ApplySuit(target, source);
        }

        public void RestorePreviousCosmetics()
        {
            PlayerControllerB target = StartOfRound.Instance.localPlayerController;
            if (target == null || previousCosmetics == null)
                return;

            RestoreCosmetics(target);
            RestoreSuit(target);

            previousCosmetics = null;
            previousSuitId = -1;
            previousUsername = null;
        }

        private void SavePreviousState(PlayerControllerB target)
        {
            previousCosmetics = new List<string>(CosmeticRegistry.locallySelectedCosmetics);
            previousSuitId = target.currentSuitID;

            previousUsername = target.playerUsername;
        }

        private void ApplyCosmetics(PlayerControllerB target, PlayerControllerB source)
        {
            if (!MainClass.playerIdsAndCosmetics.TryGetValue((int)source.playerClientId, out List<string> sourceCosmetics))
                return;

            CosmeticRegistry.locallySelectedCosmetics.Clear();
            CosmeticRegistry.locallySelectedCosmetics.AddRange(sourceCosmetics);
            CosmeticSyncPatch.SyncCosmeticsToOtherClients();
        }

        private void RestoreCosmetics(PlayerControllerB target)
        {
            target.movementAudio.PlayOneShot(StartOfRound.Instance.changeSuitSFX);

            CosmeticRegistry.locallySelectedCosmetics.Clear();
            CosmeticRegistry.locallySelectedCosmetics.AddRange(previousCosmetics);
            CosmeticSyncPatch.SyncCosmeticsToOtherClients();
        }

        [ServerRpc(RequireOwnership = false)]
        private void ApplySuit(PlayerControllerB target, PlayerControllerB source)
        {
            target.currentSuitID = source.currentSuitID;
            target.movementAudio.PlayOneShot(StartOfRound.Instance.changeSuitSFX);
            RefreshSuitModel(target);

            UnlockableSuit.SwitchSuitForPlayer(target, source.currentSuitID);

        }

        private void RestoreSuit(PlayerControllerB target)
        {
            if (previousSuitId != -1)
            {
                target.currentSuitID = previousSuitId;
                RefreshSuitModel(target);
            }
        }

        private void RefreshSuitModel(PlayerControllerB target)
        {
            var method = target.GetType().GetMethod("UpdateSuitVisuals", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (method != null)
                method.Invoke(target, null);
        }

        public static bool IsValidPlayer(PlayerControllerB player)
        {
            return player != null &&
                   player.isPlayerControlled &&
                   !string.IsNullOrEmpty(player.playerUsername);
        }
    }
}
