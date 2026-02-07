using GameNetcodeStuff;
using Lethal_Battle;
using MoreCompany;
using MoreCompany.Cosmetics;
using System.Collections.Generic;
using UnityEngine;

namespace Silly_Things.codes.MorphingCase
{
    internal class ManageCosmetics
    {
        public List<string> previousCosmetics;

        public void MorphToPlayer(PlayerControllerB source)
        {
            Plugin.log.LogInfo("[ManageCosmetics] MorphToPlayer called");

            PlayerControllerB target = StartOfRound.Instance.localPlayerController;
            if (source == null || target == null)
            {
                Plugin.log.LogError("[ManageCosmetics] source or target null");
                return;
            }

            Plugin.log.LogInfo($"[ManageCosmetics] Source: {source.playerUsername} | Target: {target.playerUsername}");

            if (!target.isPlayerControlled)
            {
                Plugin.log.LogWarning("[ManageCosmetics] Target not controlled");
                return;
            }

            target.movementAudio.PlayOneShot(StartOfRound.Instance.changeSuitSFX);
            Plugin.log.LogInfo("[ManageCosmetics] Played suit change SFX");

            target.currentSuitID = source.currentSuitID;

            Plugin.log.LogInfo($"[ManageCosmetics] Suit copied: {source.currentSuitID}");

            if (!MainClass.playerIdsAndCosmetics.TryGetValue((int)source.playerClientId, out List<string> sourceCosmetics))
            {
                Plugin.log.LogError("[ManageCosmetics] No cosmetics found for source player");
                return;
            }

            Plugin.log.LogInfo($"[ManageCosmetics] Source cosmetics count: {sourceCosmetics.Count}");

            if (previousCosmetics == null)
            {
                previousCosmetics = new List<string>(CosmeticRegistry.locallySelectedCosmetics);
                Plugin.log.LogInfo($"[ManageCosmetics] Saved previous cosmetics: {previousCosmetics.Count}");
            }

            CosmeticRegistry.locallySelectedCosmetics.Clear();
            Plugin.log.LogInfo("[ManageCosmetics] Cleared local cosmetics");

            CosmeticRegistry.locallySelectedCosmetics.AddRange(sourceCosmetics);
            Plugin.log.LogInfo("[ManageCosmetics] Applied source cosmetics locally");

            CosmeticSyncPatch.SyncCosmeticsToOtherClients();
            Plugin.log.LogInfo("[ManageCosmetics] SyncCosmeticsToOtherClients called");
        }

        public void RestorePreviousCosmetics()
        {
            Plugin.log.LogInfo("[ManageCosmetics] RestorePreviousCosmetics called");

            if (previousCosmetics == null)
            {
                Plugin.log.LogWarning("[ManageCosmetics] No previous cosmetics to restore");
                return;
            }

            CosmeticRegistry.locallySelectedCosmetics.Clear();
            Plugin.log.LogInfo("[ManageCosmetics] Cleared local cosmetics");

            CosmeticRegistry.locallySelectedCosmetics.AddRange(previousCosmetics);
            Plugin.log.LogInfo($"[ManageCosmetics] Restored cosmetics count: {previousCosmetics.Count}");

            CosmeticSyncPatch.SyncCosmeticsToOtherClients();
            Plugin.log.LogInfo("[ManageCosmetics] SyncCosmeticsToOtherClients called");

            previousCosmetics = null;
            Plugin.log.LogInfo("[ManageCosmetics] previousCosmetics reset");
        }

        public static bool IsValidPlayer(PlayerControllerB player)
        {
            bool valid =
                player != null &&
                player.isPlayerControlled &&
                !string.IsNullOrEmpty(player.playerUsername);


            if (!valid && player != null)
                Plugin.log.LogInfo($"[ManageCosmetics] Invalid player skipped: {player.playerUsername}, isplayercontrol : {player.isPlayerControlled}");
            else
            {
                Plugin.log.LogInfo($"[ManageCosmetics] Valid player: {player.playerUsername}, isplayercontrol : {player.isPlayerControlled}");
            }
            return valid;
        }
    }
}
