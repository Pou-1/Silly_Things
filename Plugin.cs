using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Modules;
using Silly_Things.codes.BountyContract;
using Silly_Things.codes.CameraItem;
using Silly_Things.codes.MorphingCase;
using Silly_Things.codes.SnakeCardboardBox;
using Silly_Things.Codes.CameraItem;
using Silly_Things.Codes.PortalGun;
using Silly_Things.Codes.SailorMoonStick;
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
        const string VERSION = "1.0.5";

        public static Plugin Instance { get; private set; } = null!;
        internal static new ManualLogSource Logger { get; private set; } = null!;
        internal static Config SillyThingsConfig { get; private set; } = null!;
        public readonly Harmony harmony = new Harmony(GUID);
        public static bool DebugMode;

        // _____________MORPHING CASE_____________ \\
        public GameObject? UI_MorphingCase;
        internal ManageCosmetics CosmeticsManager { get; private set; } = null!;
        public AudioClip SoundOpenUI = null!;
        public AudioClip SoundCloseUI = null!;

        // _____________SNAKE CARDBOARD BOX_____________ \\
        public GameObject? UI_SnakeCardboardBox;
        public GameObject? BigCardboardBoxPrefab;
        public AudioClip SoundOpenCardboardBox = null!;
        public AudioClip SoundCloseCardboardBox = null!;

        // _____________BOUNTY HUNT_____________ \\
        public GameObject? FootprintPrefab;
        public AudioClip SoundSonar = null!;
        public GameObject? UI_Bounty;

        // _____________PORTAL GUN_____________ \\
        public GameObject? PortalPrefabA;
        public GameObject? PortalPrefabB;

        // _____________CAMERA_____________ \\
        public AudioClip SoundShutter = null!;
        public AudioClip SoundGear = null!;
        public AudioClip SoundSucess = null!;
        public GameObject? PhotoItemPrefab;
        public GameObject? CameraVariantGold;
        public GameObject? CameraVariantBlue;
        public GameObject? CameraVariantBlack;
        public Shader? photoShader;
        public Shader? photoShaderBlack;
        public Shader? photoShaderBlue;
        public Shader? photoShaderGold;

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

            LoadMorphingCase(bundle);
            LoadCamera(bundle);
            //LoadBountyContract(bundle);
            //LoadSnakeCardboardBox(bundle);
            //LoadSailorMoonStick(bundle);
            //LoadPortalGun(bundle);
        }

        private T LoadItemTemplate<T>(AssetBundle bundle, string assetPath, string itemName, int rarity, bool addtoShop = false, int cost = 0, string displayText = "") where T : GrabbableObject
        {
            Item item = bundle.LoadAsset<Item>(assetPath);

            T script = item.spawnPrefab.AddComponent<T>();
            script.name = itemName;
            script.grabbable = true;
            script.grabbableToEnemies = true;
            script.itemProperties = item;

            if (SillyThingsConfig.iconCustom.Value == false)
                script.itemProperties.itemIcon = null;

            NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
            Items.RegisterScrap(item, rarity, Levels.LevelTypes.All);

            if (addtoShop)
            {
                TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
                node.clearPreviousText = true;
                node.displayText = displayText;
                Items.RegisterShopItem(item, itemInfo: node, price: cost);
            }

            return script;
        }

        public void OnApplicationQuit()
        {
            CosmeticsManager.RestorePreviousCosmetics();
        }

        public void LoadMorphingCase(AssetBundle bundle)
        {
            UI_MorphingCase = bundle.LoadAsset<GameObject>("Assets/LethalModding/MorphingCase/UI/UICase.prefab");

            SoundOpenUI = bundle.LoadAsset<AudioClip>("Assets/LethalModding/MorphingCase/Case/open.ogg");
            SoundCloseUI = bundle.LoadAsset<AudioClip>("Assets/LethalModding/MorphingCase/Case/close.ogg");

            LoadItemTemplate<MorphingCase>(bundle, "Assets/LethalModding/MorphingCase/Case/ShapeshiftCaseItem.asset", "Morphing Case", SillyThingsConfig.MorphingCaseItemRarity.Value);
        }

        public void LoadBountyContract(AssetBundle bundle)
        {
            FootprintPrefab = bundle.LoadAsset<GameObject>("Assets/LethalModding/BountyContract/FootPrintPrefab.prefab");
            SoundSonar = bundle.LoadAsset<AudioClip>("Assets/LethalModding/BountyContract/sonar.ogg");
            UI_Bounty = bundle.LoadAsset<GameObject>("Assets/LethalModding/BountyContract/UI/UIBounty.prefab");

            LoadItemTemplate<BountyContract>(bundle, "Assets/LethalModding/BountyContract/BountyContract.asset", "Bounty Contract", SillyThingsConfig.BountyContract.Value);

            string monsters = SillyThingsConfig.BountymonsterValues.Value;
            string[] monsterValuePair = monsters.Split(",");

            foreach (var mvp in monsterValuePair)
            {
                if (string.IsNullOrEmpty(mvp))
                    continue;

                var parts = mvp.Split(':');
                if (parts.Length == 3)
                {
                    int value = int.Parse(parts[1]);
                    int count = int.Parse(parts[2]);
                    HelperBountyContract.MonsterValues.Add(new HelperBountyContract.MonsterNameBounty(parts[0].ToLower(), value, count));
                }
            }
        }

        public void LoadSailorMoonStick(AssetBundle bundle)
        {
            LoadItemTemplate<SailorMoonStick>(bundle, "Assets/LethalModding/SailorMoonStick/SailorMoonStick.asset", "Sailor Moon Stick", SillyThingsConfig.BountyContract.Value);
        }

        public void LoadCamera(AssetBundle bundle)
        {
            SoundShutter = bundle.LoadAsset<AudioClip>("Assets/LethalModding/Camera/cameraShutter.ogg");
            SoundSucess = bundle.LoadAsset<AudioClip>("Assets/LethalModding/Camera/Sucess.ogg");
            SoundGear = bundle.LoadAsset<AudioClip>("Assets/LethalModding/Camera/gearCamera.ogg");

            CameraVariantGold = bundle.LoadAsset<GameObject>("Assets/LethalModding/Camera/CameraVariantGold/CameraItemGold.prefab");
            CameraVariantBlue = bundle.LoadAsset<GameObject>("Assets/LethalModding/Camera/CameraVariantBlue/CameraItemBlue.prefab");
            CameraVariantBlack = bundle.LoadAsset<GameObject>("Assets/LethalModding/Camera/CameraVariantBlack/CameraItemBlack.prefab");

            string displayText = "Take picture of monsters.\n" + "Sell them to the Company.\n" + "Live for another day of work\n\n" + "Each monster give different value to the pictures (based on their dangerosity)\n" + "Friend in the picture with monsters bring more value to it\n";

            LoadItemTemplate<CameraItem>(bundle, "Assets/LethalModding/Camera/CameraItem.asset", "Camera", SillyThingsConfig.cameraLootRarity.Value, addtoShop: SillyThingsConfig.cameraCanBeBuy.Value, cost: SillyThingsConfig.cameraCost.Value, displayText: displayText);

            PhotoItemPrefab = LoadItemTemplate<PhotoItem>(bundle, "Assets/LethalModding/Camera/PhotoItem/PhotoItem.asset", "Picture", 0).gameObject;

            HelperCamera.LoadMonstersValues();

            photoShader = bundle.LoadAsset<Shader>("Assets/LethalModding/Camera/ShaderCamera.shader");
            photoShaderBlack = bundle.LoadAsset<Shader>("Assets/LethalModding/Camera/CameraVariantBlack/ShaderCameraBlack.shader");
            photoShaderGold = bundle.LoadAsset<Shader>("Assets/LethalModding/Camera/CameraVariantGold/ShaderCameraGold.shader");
            photoShaderBlue = bundle.LoadAsset<Shader>("Assets/LethalModding/Camera/CameraVariantBlue/ShaderCameraBlue.shader");

            if (SillyThingsConfig.DeletePictureOnLaunch.Value)
                HelperCamera.DeletePictures();
        }
        
        public void LoadSnakeCardboardBox(AssetBundle bundle)
        {
            BigCardboardBoxPrefab = bundle.LoadAsset<GameObject>("Assets/LethalModding/SnakeCardboardBox/BoxOnPlayer/CardBoardModel.prefab");
            UI_SnakeCardboardBox = bundle.LoadAsset<GameObject>("Assets/LethalModding/SnakeCardboardBox/UI/CardboardBox.prefab");
            SoundOpenCardboardBox = bundle.LoadAsset<AudioClip>("Assets/LethalModding/SnakeCardboardBox/SnakeCardboardBox/open.ogg");
            SoundCloseCardboardBox = bundle.LoadAsset<AudioClip>("Assets/LethalModding/SnakeCardboardBox/SnakeCardboardBox/close.ogg");

            LoadItemTemplate<SnakeCardboardBox>(bundle, "Assets/LethalModding/SnakeCardboardBox/SnakeCardboardBox/CardBoardBoxItem.asset", "A simple cardboard", SillyThingsConfig.SnakeCardboardBox.Value);
        }

        public void LoadPortalGun(AssetBundle bundle)
        {
            LoadPortalPrefab(bundle);
            LoadItemTemplate<PortalGun>(bundle, "Assets/LethalModding/PortalGun/PortalGunItem.asset", "Portal Gun", SillyThingsConfig.SnakeCardboardBox.Value);
        }

        public void LoadPortalPrefab(AssetBundle bundle)
        {
            PortalPrefabA = bundle.LoadAsset<GameObject>("Assets/LethalModding/PortalGun/portal/PortalA.prefab");
            if (PortalPrefabA == null)
            {
                Logger.LogError("Portal A prefab not found!");
                return;
            }

            if (PortalPrefabA.GetComponent<Portal>() == null)
                PortalPrefabA.AddComponent<Portal>();

            Rigidbody rb = PortalPrefabA.GetComponent<Rigidbody>();
            if (rb == null)
                rb = PortalPrefabA.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Collider col = PortalPrefabA.GetComponent<Collider>();
            if (col == null)
                col = PortalPrefabA.AddComponent<BoxCollider>();
            col.isTrigger = true;

            NetworkPrefabs.RegisterNetworkPrefab(PortalPrefabA);

            PortalPrefabB = bundle.LoadAsset<GameObject>("Assets/LethalModding/PortalGun/portal/PortalB.prefab");
            if (PortalPrefabB == null)
            {
                Logger.LogError("Portal B prefab not found!");
                return;
            }

            if (PortalPrefabB.GetComponent<Portal>() == null)
                PortalPrefabB.AddComponent<Portal>();

            Rigidbody rb2 = PortalPrefabB.GetComponent<Rigidbody>();
            if (rb == null)
                rb = PortalPrefabB.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Collider col2 = PortalPrefabB.GetComponent<Collider>();
            if (col == null)
                col = PortalPrefabB.AddComponent<BoxCollider>();
            col.isTrigger = true;

            NetworkPrefabs.RegisterNetworkPrefab(PortalPrefabB);
        }
    }
}
