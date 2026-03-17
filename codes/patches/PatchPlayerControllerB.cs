using GameNetcodeStuff;
using HarmonyLib;

namespace Silly_Things.codes.patches
{
    /*[HarmonyPatch(typeof(PlayerControllerB))]
    internal class PatchPlayerControllerB
    {
        [HarmonyPrefix]
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
        }
    }*/
}
