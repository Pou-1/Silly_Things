using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using Silly_Things.codes.BountyContract;

namespace Silly_Things.codes.patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal class Patch_EnemyAI_PlayerIsTargetable
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetAllPlayersInLineOfSight")]
        public static void BlockLOSWhenInSnakeBox(ref PlayerControllerB[] __result)
        {
            if(__result == null)
                return;

            foreach (SnakeCardboardBox.SnakeCardboardBox item in SnakeCardboardBox.SnakeCardboardBox.Instances)
            {
                if (item.PlayerHiddenByBox)
                {
                    if (item.playerHeldBy != null && __result.Contains(item.playerHeldBy))
                    {
                        __result = __result.Where(player => player != item.playerHeldBy).ToArray();
                    }
                }
            }

            //Plugin.Logger.LogInfo("LOS BLOCKED");
        }

        [HarmonyPostfix]
        [HarmonyPatch("CheckLineOfSightForPlayer")]
        public static void CheckLineOfSightForPlayerPrefixPatch(ref PlayerControllerB __result)
        {
            if (__result == null)
                return;
            
            foreach (SnakeCardboardBox.SnakeCardboardBox item in SnakeCardboardBox.SnakeCardboardBox.Instances)
            {
                if (item.PlayerHiddenByBox && __result == item.playerHeldBy)
                {
                    __result = null;
                    //Plugin.Logger.LogInfo("LOS one player BLOCKED");
                    return;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("CheckLineOfSightForClosestPlayer")]
        public static void CheckLineOfSightForClosestPlayerPrefixPatch(ref PlayerControllerB __result)
        {
            if (__result == null)
                return;
            
            foreach (var item in SnakeCardboardBox.SnakeCardboardBox.Instances)
            {
                if (item.PlayerHiddenByBox && __result == item.playerHeldBy)
                {
                    __result = null;
                    //Plugin.Logger.LogInfo("CheckLineOfSightForClosestPlayer BLOCKED");
                    return;
                }
            }
        }
    }
}
