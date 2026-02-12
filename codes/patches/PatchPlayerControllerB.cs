using GameNetcodeStuff;
using HarmonyLib;
using Silly_Things.codes.SnakeCardboardBox;

namespace Silly_Things.codes.patches
{
    /*[HarmonyPatch(typeof(PlayerControllerB))]
    internal class PatchPlayerControllerB
    {
        [HarmonyPrefix]
        [HarmonyPatch("Crouch")]
        public static bool BlockUncrouchWhileUsingBox(bool crouch)
        {
            if (!crouch && SnakeCardboardBox.SnakeCardboardBox.Instance != null)
            {
                var uiField = typeof(SnakeCardboardBox.SnakeCardboardBox)
                    .GetField("ui", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (uiField != null)
                {
                    var ui = uiField.GetValue(SnakeCardboardBox.SnakeCardboardBox.Instance) as SnakeCardboardBoxUi;
                    if (ui != null && ui.IsOpen)
                        return false;
                }
            }

            return true;
        }
    }*/
}
