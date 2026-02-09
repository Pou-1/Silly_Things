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

        internal ManageCosmetics CosmeticsManager { get; private set; } = null!;

        public AudioClip SoundOpenUI = null!;
        public AudioClip SoundCloseUI = null!;

        public void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            SillyThingsConfig = new Config(Config);
            CosmeticsManager = new ManageCosmetics();

            if (PlayerPrefs.HasKey("MorphingCase_PreviousCosmetics"))
            {
                CosmeticsManager.RestorePreviousCosmetics();
                PlayerPrefs.DeleteKey("MorphingCase_PreviousCosmetics");
                PlayerPrefs.DeleteKey("MorphingCase_PreviousSuit");
            }

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
            LoadAudioSourceMorphingCase(bundle);
        }

        public void LoadAudioSourceMorphingCase(AssetBundle bundle)
        {
            SoundOpenUI = bundle.LoadAsset<AudioClip>("Assets/LethalModding/sounds/open.ogg");
            SoundCloseUI = bundle.LoadAsset<AudioClip>("Assets/LethalModding/sounds/close.ogg");

            if (SoundCloseUI == null || SoundOpenUI == null)
                Logger.LogError("Sounds Morphing Case load fail");
        }

        public void OnApplicationQuit()
        {
            CosmeticsManager.RestorePreviousCosmetics();
        }

        public void LoadUIMorphingCase(AssetBundle bundle)
        {
            UI_MorphingCase = bundle.LoadAsset<GameObject>("Assets/LethalModding/MorphingCase/UI/UICase.prefab");

            if (UI_MorphingCase == null)
                Logger.LogError("UI Morphing Case load fail");
        }

        public void LoadMorphingCase(AssetBundle bundle)
        {
            Item morphingCase = bundle.LoadAsset<Item>("Assets/LethalModding/MorphingCase/Case/ShapeshiftCaseItem.asset");
            if (morphingCase == null)
            {
                Logger.LogError("Morphing Case is NULL");
                return;
            }

            if (morphingCase.spawnPrefab == null)
            {
                Logger.LogError("Morphing Case spawnPrefab is NULL");
                return;
            }

            MorphingCase script = morphingCase.spawnPrefab.AddComponent<MorphingCase>();
            script.grabbable = true;
            script.grabbableToEnemies = true;
            script.itemProperties = morphingCase;

            NetworkPrefabs.RegisterNetworkPrefab(morphingCase.spawnPrefab);
            Utilities.FixMixerGroups(morphingCase.spawnPrefab);
            Items.RegisterScrap(morphingCase, SillyThingsConfig.MorphingCaseItemRarity.Value, Levels.LevelTypes.All);

            Logger.LogInfo("Morphing Case loaded successfully");
        }
    }
}
