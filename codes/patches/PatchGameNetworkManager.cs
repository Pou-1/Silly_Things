using BepInEx;
using HarmonyLib;
using System.IO;

namespace Silly_Things.codes.patches
{
    /*[HarmonyPatch(typeof(GameNetworkManager))]
    internal class PatchGameNetworkManager
    {
        [HarmonyPrefix]
        [HarmonyPatch("AAAAAAAAAAAAAAAAAAA ON SAVE DELETE IDK THE NAME")]
        public static void OnDeleteSave(int saveFileNum)
        {
            string saveName = $"LCSaveFile{saveFileNum}";
            string folder = Path.Combine(Paths.GameRootPath, "CameraPictures", saveName);

            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
                Plugin.Logger.LogInfo($"Deleted photo folder: {folder}");
            }
        }
    }*/
}
