using GameNetcodeStuff;
using MoreCompany;
using MoreCompany.Cosmetics;
using System.Collections.Generic;

namespace Silly_Things.codes.MorphingCase
{
    internal class ManageCosmetics
    {
        private List<string> previousCosmetics;
        private int previousSuitId = -1;

        public void MorphToPlayer(PlayerControllerB source)
        {
            PlayerControllerB target = StartOfRound.Instance.localPlayerController;
            if (source == null || target == null)
                return;
            if (!target.isPlayerControlled)
                return;

            if (previousCosmetics == null)
            {
                previousCosmetics = new List<string>(CosmeticRegistry.locallySelectedCosmetics);
                previousSuitId = target.currentSuitID;
                Plugin.Logger.LogError("previousCosmetics");
                Plugin.Logger.LogError(previousCosmetics);
            }

            target.movementAudio.PlayOneShot(StartOfRound.Instance.changeSuitSFX);
            target.currentSuitID = source.currentSuitID;

            if (!MainClass.playerIdsAndCosmetics.TryGetValue((int)source.playerClientId, out List<string> sourceCosmetics))
                return;

            CosmeticRegistry.locallySelectedCosmetics.Clear();
            CosmeticRegistry.locallySelectedCosmetics.AddRange(sourceCosmetics);

            CosmeticSyncPatch.SyncCosmeticsToOtherClients();
        }

        public void RestorePreviousCosmetics()
        {
            if (previousCosmetics == null)
                return;

            PlayerControllerB target = StartOfRound.Instance.localPlayerController;

            Plugin.Logger.LogError("restore");
            Plugin.Logger.LogError(CosmeticRegistry.locallySelectedCosmetics);
            CosmeticRegistry.locallySelectedCosmetics.Clear();
            Plugin.Logger.LogError(CosmeticRegistry.locallySelectedCosmetics);
            CosmeticRegistry.locallySelectedCosmetics.AddRange(previousCosmetics);
            Plugin.Logger.LogError(CosmeticRegistry.locallySelectedCosmetics);

            if (previousSuitId != -1)
                target.currentSuitID = previousSuitId;

            CosmeticSyncPatch.SyncCosmeticsToOtherClients();

            Plugin.Logger.LogError(CosmeticRegistry.GetCosmeticsToSync());
            previousCosmetics = null;
            previousSuitId = -1;
        }

        public static bool IsValidPlayer(PlayerControllerB player)
        {
            return player != null &&
                   player.isPlayerControlled &&
                   !string.IsNullOrEmpty(player.playerUsername);
        }
    }
}