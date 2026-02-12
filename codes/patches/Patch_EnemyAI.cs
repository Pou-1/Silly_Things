using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace Silly_Things.codes.patches
{
    /*[HarmonyPatch(typeof(EnemyAI))]
    internal class Patch_EnemyAI_PlayerIsTargetable
    {
        [HarmonyPrefix]
        [HarmonyPatch("PlayerIsTargetable")]
        public static bool BlockTargetingWhileInSnakeBox(ref bool __result, PlayerControllerB playerScript, bool cannotBeInShip, bool overrideInsideFactoryCheck)
        {
            if (SnakeCardboardBox.SnakeCardboardBox.PlayerHiddenByBox &&
                playerScript == StartOfRound.Instance.localPlayerController)
            {
                Plugin.Logger.LogInfo("PlayerIsTargetable BLOCKED");
                __result = false;
                return false;
            }

            Plugin.Logger.LogInfo("PlayerIsTargetable NOT BLOCKED");
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("GetAllPlayersInLineOfSight")]
        public static bool BlockLOSWhenInSnakeBox(ref PlayerControllerB[] __result, float width, int range, Transform eyeObject, float proximityCheck, int layerMask)
        {
            if (!SnakeCardboardBox.SnakeCardboardBox.PlayerHiddenByBox){
                Plugin.Logger.LogInfo("LOS NOT BLOCKED");
                return true;
            }

            Plugin.Logger.LogInfo("LOS BLOCKED");

            __result = null;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("SetMovingTowardsTargetPlayer")]
        public static bool BlockMovingTowardsTargetPlayer(PlayerControllerB playerScript)
        {
            if (SnakeCardboardBox.SnakeCardboardBox.PlayerHiddenByBox &&
                playerScript == StartOfRound.Instance.localPlayerController)
            {
                Plugin.Logger.LogInfo("BlockMovingTowardsTargetPlayer BLOCKED");
                return false;
            }

            Plugin.Logger.LogInfo("BlockMovingTowardsTargetPlayer NOT BLOCKED");
            return true;
        }
    }*/
}
