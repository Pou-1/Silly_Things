using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Modules;
using Silly_Things.codes.MorphingCase;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Silly_Things
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        const string GUID = "POUY.SILLY_THINGS";
        const string NAME = "Silly Things";
        const string VERSION = "0.0.1";

        public static Plugin Instance { get; private set; } = null!;
        internal static new ManualLogSource Logger { get; private set; } = null!;
        public readonly Harmony harmony = new Harmony(GUID);
        internal static Config SillyThingsConfig { get; private set; } = null!;

        public GameObject? UI_MorphingCase;

        public AudioSource? SoundOpenUI;
        public AudioSource? SoundCloseUI;

        public void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            SillyThingsConfig = new Config(Config);

            LoadAssetBundle();

            Logger.LogMessage("Silly things Loaded !");

            harmony.PatchAll();
        }

        private void LoadAssetBundle()
        {
            string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assetBundlePath = Path.Combine(assemblyLocation!, "sillythings");

            AssetBundle bundle = AssetBundle.LoadFromFile(assetBundlePath);
            if (bundle == null)
            {
                Logger.LogError("Failed to load asset bundle");
                return;
            }
            LoadUIMorphingCase(bundle);
            LoadMorphingCase(bundle);
        }

        public void LoadAudioSourceMorphingCase(AssetBundle bundle)
        {
            SoundOpenUI = bundle.LoadAsset<AudioSource>("Assets/LethalModding/sounds/UI/close.ogg");
            SoundCloseUI = bundle.LoadAsset<AudioSource>("Assets/LethalModding/sounds/UI/open.ogg");

            if (SoundCloseUI == null || SoundOpenUI == null)
            {
                Logger.LogError("Sounds Morphing Case load fail");
            }
            Logger.LogInfo("Sounds Morphing Case loaded successfully");
        }

        public void LoadUIMorphingCase(AssetBundle bundle)
        {
            UI_MorphingCase = bundle.LoadAsset<GameObject>("Assets/LethalModding/MorphingCase/UI/UICase.prefab");

            if (UI_MorphingCase == null)
            {
                Logger.LogError("UI Morphing Case load fail");
            }
            Logger.LogInfo("UI Morphing Case loaded successfully");
        }

        public void LoadMorphingCase(AssetBundle bundle)
        {
            Item morphingCase = bundle.LoadAsset<Item>("Assets/LethalModding/MorphingCase/Case/ShapeshiftCaseItem.asset");
            if (morphingCase == null)
            {
                Logger.LogError("Morphing Case is NULL");
            }
            else
            {
                if (morphingCase.spawnPrefab == null)
                    Logger.LogError("Morphing Case spawnPrefab is NULL");
                else
                {
                    MorphingCase script = morphingCase.spawnPrefab.AddComponent<MorphingCase>();
                    script.grabbable = true;
                    script.grabbableToEnemies = true;
                    script.itemProperties = morphingCase;
                }
            }

            NetworkPrefabs.RegisterNetworkPrefab(morphingCase.spawnPrefab);
            Utilities.FixMixerGroups(morphingCase.spawnPrefab);
            Items.RegisterScrap(morphingCase, SillyThingsConfig.MorphingCaseItemRarity.Value, Levels.LevelTypes.All);

            /*TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "this is silly case";
            Items.RegisterShopItem(morphingCase, null, null, node, 320);*/

            Logger.LogInfo("Morphing Case loaded successfully");
        }
    }
}
