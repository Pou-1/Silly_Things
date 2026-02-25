using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Modules;
using Silly_Things.codes.MorphingCase;
using Silly_Things.codes.SnakeCardboardBox;
using Silly_Things.Codes.PortalGun;
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
        const string VERSION = "0.0.2";

        public static Plugin Instance { get; private set; } = null!;
        internal static new ManualLogSource Logger { get; private set; } = null!;
        internal static Config SillyThingsConfig { get; private set; } = null!;
        public readonly Harmony harmony = new Harmony(GUID);

        //Morphing Case
        public GameObject? UI_MorphingCase;
        internal ManageCosmetics CosmeticsManager { get; private set; } = null!;
        public AudioClip SoundOpenUI = null!;
        public AudioClip SoundCloseUI = null!;

        //Snake CardBoard
        public GameObject? UI_SnakeCardboardBox;
        public GameObject? BigCardboardBoxPrefab;
        public AudioClip SoundOpenCardboardBox = null!;
        public AudioClip SoundCloseCardboardBox = null!;
        
        //Portal Gun
        public GameObject? PortalPrefab;

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

            LoadAudioSource(bundle);
            LoadMorphingCase(bundle);
            LoadSnakeCardboardBox(bundle);
            //LoadPortalGun(bundle);
        }

        public void LoadAudioSource(AssetBundle bundle)
        {
            SoundOpenUI = bundle.LoadAsset<AudioClip>("Assets/LethalModding/MorphingCase/Case/open.ogg");
            SoundCloseUI = bundle.LoadAsset<AudioClip>("Assets/LethalModding/MorphingCase/Case/close.ogg");
            SoundOpenCardboardBox = bundle.LoadAsset<AudioClip>("Assets/LethalModding/SnakeCardboardBox/SnakeCardboardBox/open.ogg");
            SoundCloseCardboardBox = bundle.LoadAsset<AudioClip>("Assets/LethalModding/SnakeCardboardBox/SnakeCardboardBox/close.ogg");
        }

        public void OnApplicationQuit()
        {
            CosmeticsManager.RestorePreviousCosmetics();
        }

        public void LoadMorphingCase(AssetBundle bundle)
        {
            UI_MorphingCase = bundle.LoadAsset<GameObject>("Assets/LethalModding/MorphingCase/UI/UICase.prefab");
            Item morphingCase = bundle.LoadAsset<Item>("Assets/LethalModding/MorphingCase/Case/ShapeshiftCaseItem.asset");
            MorphingCase script = morphingCase.spawnPrefab.AddComponent<MorphingCase>();
            script.name = "Morphing Case";
            script.grabbable = true;
            script.grabbableToEnemies = true;
            script.itemProperties = morphingCase;
            NetworkPrefabs.RegisterNetworkPrefab(morphingCase.spawnPrefab);
            Items.RegisterScrap(morphingCase, SillyThingsConfig.MorphingCaseItemRarity.Value, Levels.LevelTypes.All);
        }

        public void LoadSnakeCardboardBox(AssetBundle bundle)
        {
            BigCardboardBoxPrefab = bundle.LoadAsset<GameObject>("Assets/LethalModding/SnakeCardboardBox/BoxOnPlayer/CardBoardModel.prefab");

            UI_SnakeCardboardBox = bundle.LoadAsset<GameObject>("Assets/LethalModding/SnakeCardboardBox/UI/CardboardBox.prefab");

            Item snakeCardboardBox = bundle.LoadAsset<Item>("Assets/LethalModding/SnakeCardboardBox/SnakeCardboardBox/CardBoardBoxItem.asset");
            SnakeCardboardBox script = snakeCardboardBox.spawnPrefab.AddComponent<SnakeCardboardBox>();
            script.grabbable = true;
            script.name = "A simple cardboard";
            script.grabbableToEnemies = true;
            script.itemProperties = snakeCardboardBox;
            NetworkPrefabs.RegisterNetworkPrefab(snakeCardboardBox.spawnPrefab);
            Items.RegisterScrap(snakeCardboardBox, SillyThingsConfig.SnakeCardboardBox.Value, Levels.LevelTypes.All);
        }

        public void LoadPortalGun(AssetBundle bundle)
        {
            LoadPortalPrefab(bundle);
            Item portalGunItem = bundle.LoadAsset<Item>("Assets/LethalModding/PortalGun/PortalGunItem.asset");
            PortalGun script = portalGunItem.spawnPrefab.AddComponent<PortalGun>();
            script.grabbable = true;
            script.name = "Portal Gun";
            script.grabbableToEnemies = true;
            script.itemProperties = portalGunItem;

            NetworkPrefabs.RegisterNetworkPrefab(portalGunItem.spawnPrefab);
            Items.RegisterScrap(portalGunItem, SillyThingsConfig.SnakeCardboardBox.Value, Levels.LevelTypes.All);
        }

        public void LoadPortalPrefab(AssetBundle bundle)
        {
            PortalPrefab = bundle.LoadAsset<GameObject>("Assets/LethalModding/PortalGun/portal/Portal.prefab");
            if (PortalPrefab == null)
            {
                Logger.LogError("Portal prefab not found!");
                return;
            }

            if (PortalPrefab.GetComponent<Portal>() == null)
                PortalPrefab.AddComponent<Portal>();

            Rigidbody rb = PortalPrefab.GetComponent<Rigidbody>();
            if (rb == null)
                rb = PortalPrefab.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Collider col = PortalPrefab.GetComponent<Collider>();
            if (col == null)
                col = PortalPrefab.AddComponent<BoxCollider>();
            col.isTrigger = true;

            NetworkPrefabs.RegisterNetworkPrefab(PortalPrefab);
        }
    }
}
