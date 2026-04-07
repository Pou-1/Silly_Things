using GameNetcodeStuff;
using HarmonyLib;
using Silly_Things.codes.CameraItem;
using Silly_Things.Codes.CameraItem;
using Unity.Netcode;

namespace Silly_Things.codes.patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class PatchStartOfRound
    {
        /*[HarmonyPrefix]
        [HarmonyPatch("Crouch")]
        public static bool BlockUncrouchWhileUsingBox(PlayerControllerB __instance, bool crouch)
        {
            if (__instance.currentlyHeldObjectServer is SnakeCardboardBox.SnakeCardboardBox box)
            {
                if (box.PlayerHiddenByBox)
                {
                    return false;
                }
            }
            return true;
        }*/

        /*[HarmonyPrefix]
        [HarmonyPatch("ShipLeave")]
        public static void StopSearchBounty()
        {
            Plugin.Logger.LogError("StopSearchBounty");
            Plugin.Logger.LogError("StopSearchBounty");
            Plugin.Logger.LogError("StopSearchBounty");
            Plugin.Logger.LogError("StopSearchBounty");
            Plugin.Logger.LogError("StopSearchBounty");

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player != null && player.isPlayerControlled && !player.isPlayerDead)
                {
                    if (player.currentlyHeldObjectServer is BountyContract.BountyContract bounty)
                    {
                        bounty.canSearchTarget = false;
                        Plugin.Logger.LogError("StopAllBountyLogic");
                        bounty.StopAllBountyLogic();
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnShipLandedMiscEvents")]
        public static void StartSearchBounty(PlayerControllerB __instance)
        {
            Plugin.Logger.LogError("StartSearchBounty");
            Plugin.Logger.LogError("StartSearchBounty");
            Plugin.Logger.LogError("StartSearchBounty");
            Plugin.Logger.LogError("StartSearchBounty");
            Plugin.Logger.LogError("StartSearchBounty");

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player != null && player.isPlayerControlled && !player.isPlayerDead)
                {
                    if (player.currentlyHeldObjectServer is BountyContract.BountyContract bounty)
                    {
                        bounty.canSearchTarget = true;
                        Plugin.Logger.LogError("RestartBountySearch");
                        bounty.RestartBountySearch();
                    }
                }
            }
        }*/

        [HarmonyPostfix]
        [HarmonyPatch("OnClientConnect")]
        public static void SendPicturesClient(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsHost || HelperCamera.clientsLoadedPhotos.Contains(clientId))
                return;

            HelperCamera.clientsLoadedPhotos.Add(clientId);

            foreach (PhotoItem photo in PhotoItem.Instances)
            {
                if (photo == null)
                    continue;

                photo.SendPicturesHostToClient(clientId, photo.UniqueIdNet.Value);
            }

            //Plugin.Logger.LogError($"Sent photos to client {clientId}");
        }

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        public static void PicturesCanBeLoaded()
        {
            if (HelperCamera.canLoadPictures == false)
                return;

            //Plugin.Logger.LogError("Start");
            HelperCamera.canLoadPictures = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnShipLandedMiscEvents")]
        public static void LoadPhotosOnGameStart()
        {
            if (!NetworkManager.Singleton.IsHost)
                return;

            if (HelperCamera.canLoadPictures)
            {
                //Plugin.Logger.LogError("LoadPhotosOnGameStart");
                HelperCamera.LoadAllPhotosFromDisk();
                HelperCamera.canLoadPictures = false;
            }
        }
    }
}
