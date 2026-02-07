using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Modules;
using Silly_Things.codes.MorphingCase;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace Lethal_Battle
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        const string GUID = "POUY.SILLY_THINGS";
        const string NAME = "Silly Things";
        const string VERSION = "0.0.1";
        public static Plugin instance;
        public static ManualLogSource log;

        public readonly Harmony harmony = new Harmony(GUID);

        public GameObject? UI_MorphingCase;

        public void Awake()
        {
            instance = this;
            log = Logger;

            LoadItem();
            LoadUIMorphingCase();

            log.LogMessage("Silly things Loaded !");

            harmony.PatchAll();
        }

        public void LoadUIMorphingCase()
        {
            try
            {
                string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "uicase");
                AssetBundle bundle = AssetBundle.LoadFromFile(assetDir);
                string path = "Assets/LethalModding/MorphingCase/UI/UICase.prefab";

                UI_MorphingCase = bundle.LoadAsset<GameObject>(path);

                log.LogInfo("UI Case loaded successfully");
            }
            catch (Exception e)
            {
                log.LogError("UI Case ERROR");
                log.LogError(e);
            }
        }

        public void LoadItem()
        {
            string assetDirPhone = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "shapeshiftcase"
            );

            AssetBundle bundlePhone = AssetBundle.LoadFromFile(assetDirPhone);
            if (bundlePhone == null)
            {
                log.LogError("Morphing Item bundle not found");
                return;
            }

            Item MorphingCaseItem = bundlePhone.LoadAsset<Item>("Assets/LethalModding/MorphingCase/Case/ShapeshiftCaseItem.asset");
            if (MorphingCaseItem == null)
            {
                log.LogError("Morphing Item is NULL");
            }
            else
            {
                if (MorphingCaseItem.spawnPrefab == null)
                    log.LogError("spawnPrefab is NULL");
                else
                {
                    log.LogInfo("spawnPrefab OK");
                    MorphingCase script = MorphingCaseItem.spawnPrefab.AddComponent<MorphingCase>();
                    script.grabbable = true;
                    script.grabbableToEnemies = true;
                    script.itemProperties = MorphingCaseItem;
                }
            }

            NetworkPrefabs.RegisterNetworkPrefab(MorphingCaseItem.spawnPrefab);
            Utilities.FixMixerGroups(MorphingCaseItem.spawnPrefab);
            Items.RegisterScrap(MorphingCaseItem, 15, Levels.LevelTypes.All);

            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "this is silly case";
            Items.RegisterShopItem(MorphingCaseItem, null, null, node, 320);

            log.LogInfo("Morphing Item item loaded successfully");
        }
    }
}
