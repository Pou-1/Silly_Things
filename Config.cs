using BepInEx.Configuration;
using System.Reflection;

namespace Silly_Things
{
    public class Config
    {
        public ConfigEntry<bool> debugMode;

        // _____________MORPHING CASE_____________ \\
        public ConfigEntry<int> MorphingCaseItemRarity;

        // _____________SNAKE CARDBOARD BOX_____________ \\
        public ConfigEntry<int> SnakeCardboardBox;

        // _____________BOUNTY_____________ \\
        public ConfigEntry<int> BountyContract;
        public ConfigEntry<int> BountyChanceToFocusPlayer;
        public ConfigEntry<int> BountyRewardForKillingPlayer;

        // _____________CAMERA_____________ \\
        public ConfigEntry<bool> DeletePictureOnLaunch;
        public ConfigEntry<float> cameraFov;
        public ConfigEntry<bool> cameraCanUpdateScreen;
        public ConfigEntry<float> cameraScreenUpdateRate;
        public ConfigEntry<float> cameraCameraFarClipping;
        public ConfigEntry<float> cameraUseCooldown;
        public ConfigEntry<int> cameraLootRarity;
        public ConfigEntry<bool> cameraCanBeBuy;
        public ConfigEntry<int> cameraCost;

        // _____________CAMERA MOB_____________ \\
        public ConfigEntry<string> monsterValues;
        public ConfigEntry<int> defaultMonsterValue;
        public ConfigEntry<int> monsterValueMultiplier;
        public ConfigEntry<bool> monsterReactToFlash;

        // _____________FLASH_____________ \\
        public ConfigEntry<float> flashAngle;
        public ConfigEntry<float> flashIntensity;
        public ConfigEntry<float> flashRange;
        public ConfigEntry<float> flashDuration;
        public ConfigEntry<float> pictureTakenAtFlashPercentage;

        // _____________PICTURE_____________ \\
        public ConfigEntry<int> pictureResolutionWidth;
        public ConfigEntry<int> pictureResolutionHeight;
        public ConfigEntry<int> pictureResolutionDepth;

        // _____________SCREEN_____________ \\
        public ConfigEntry<int> screenResolutionHeight;
        public ConfigEntry<int> screenResolutionWidth;
        public ConfigEntry<int> screenResolutionDepth;

        public Config(ConfigFile configFile)
        {
            configFile.SaveOnConfigSet = false;

            debugMode = configFile.Bind(
                "Debug",
                "debugMode",
                false,
                "More log"
            );

            MorphingCaseItemRarity = configFile.Bind(
                "Spawn Rates",
                "MorphingCaseItemRarity",
                15,
                "Rarity of the Morphing Case Item (higher = more common)."
            );

            SnakeCardboardBox = configFile.Bind(
                "Spawn Rates",
                "SnakeCardboardBoxItemRarity",
                10,
                "Rarity of the Snake Card Box Item (higher = more common)."
            );

            BountyContract = configFile.Bind(
                "Spawn Rates",
                "BountyContractItemRarity",
                15,
                "Rarity of the Bounty Contract Item (higher = more common)."
            );

            BountyChanceToFocusPlayer = configFile.Bind(
                "Chance target player in bounty",
                "BountyChanceToFocusPlayer",
                25,
                "Percentage of chance for a bounty contract to focus a player (higher = more chance)."
            );

            BountyRewardForKillingPlayer = configFile.Bind(
                "Rewards for killing a player in bounty",
                "BountyRewardForKillingPlayer",
                100,
                "Amount of Money earn when a player is killed in a bounty contract (higher = more money)."
            );

            // _____________CAMERA_____________ \\
            DeletePictureOnLaunch = configFile.Bind(
                "Files",
                "DeletePictureOnLaunch",
                false,
                "Are all the picture getting deleted on every launch of lethal company"
            );

            monsterValues = configFile.Bind(
                "Gameplay",
                "Monsters value",
                "" + MakeMonsterString(),
                "Assign a monster name to a value monsterName1:scrapValue1,monsterName2:scrapValue2,... (Can be used to overide base monster values too)"
            );

            defaultMonsterValue = configFile.Bind(
                "Gameplay",
                "Default monster value",
                10,
                "Change default value for monster not listed above (MonsterValues)"
            );

            monsterValueMultiplier = configFile.Bind(
                "Gameplay",
                "Monster value multiplier",
                1,
                "Change this value to multiply the value of all pictures"
            );

            monsterReactToFlash = configFile.Bind(
                "Gameplay",
                "Monster React to flash",
                true,
                "Does monster have special reaction when takken in picture"
            );

            cameraLootRarity = configFile.Bind(
                "Gameplay",
                "CameraLootRarity",
                15,
                "Chance of looting a camera (1 very rare, 100 very common)"
            );

            cameraCanBeBuy = configFile.Bind(
                "Gameplay",
                "CameraCanBeBuy",
                true,
                "Does the camera appear in the store ?"
            );

            cameraCost = configFile.Bind(
                "Gameplay",
                "CameraCost",
                150,
                "The cost of the camera in the store"
            );

            cameraFov = configFile.Bind(
                "Flash",
                "CameraFov",
                75f,
                "The Field of view of the camera"
            );

            flashAngle = configFile.Bind(
                "Flash",
                "FlashAngle",
                160f,
                "The wideness of the flash (in degree)"
            );

            flashIntensity = configFile.Bind(
                "Flash",
                "FlashIntensity",
                250f,
                "The intensity of the flash"
            );

            flashRange = configFile.Bind(
                "Flash",
                "FlashRange",
                50f,
                "The range of the flash in meter"
            );

            flashDuration = configFile.Bind(
                "Flash",
                "FlashDuration",
                0.8f,
                "The duration of the flash in seconds (How long the intensity goes back to 0)"
            );

            pictureTakenAtFlashPercentage = configFile.Bind(
                "Flash",
                "PictureTakenAtFlashPercentage",
                0.5f,
                "At which percentage of the flash duration the picture is actually taken (the intensity when the picture is taken is : flashIntensity / (1-pictureTakenAtFlashPercentage) )"
            );

            pictureResolutionWidth = configFile.Bind(
                "Camera",
                "PictureResolutionWidth",
                1920,
                "The width of the picture (careful can heavily impact performance when taking picture)"
            );

            pictureResolutionHeight = configFile.Bind(
               "Camera",
               "pictureResolutionHeight",
               1080,
               "The height of the picture (careful can heavily impact performance when taking picture)"
            );

            pictureResolutionDepth = configFile.Bind(
                "Camera",
                "pictureResolutionDepth",
                50,
                "The resolution of the camera screen 128,256,512,1024... (careful can heavily impact performance when the camera is held)"
            );

            screenResolutionWidth = configFile.Bind(
                "Camera",
                "screenResolutionWidth",
                240,
                "The resolution of the camera screen 128,256,512,1024... (careful can heavily impact performance when the camera is held)"
            );

            screenResolutionHeight = configFile.Bind(
                "Camera",
                "screenResolutionHeight",
                160,
                "The resolution of the camera screen 128,256,512,1024... (careful can heavily impact performance when the camera is held)"
            );

            screenResolutionDepth = configFile.Bind(
                "Camera",
                "screenResolutionDepth",
                50,
                "The resolution of the camera screen 128,256,512,1024... (careful can heavily impact performance when the camera is held)"
            );

            cameraCanUpdateScreen = configFile.Bind(
                "Camera",
                "CameraCanUpdateScreen",
                true,
                "Does the camera preview the picture in its screen ? (disable it to improve performance)"
            );

            cameraScreenUpdateRate = configFile.Bind(
                "Camera",
                "CameraScreenUpdateRate",
                0.5f,
                "The delay betway camera screen update in seconds (increase this if you have performance issue)"
            );

            cameraCameraFarClipping = configFile.Bind(
                "Camera",
                "CameraCameraFarClipping",
                100.0f,
                "How far the camera camera can render (reduce this if you have performance issue)"
            );

            cameraUseCooldown = configFile.Bind(
                "Camera",
                "CameraUseCooldown",
                1f,
                "The required cooldown between 2 pictures"
            );


            configFile.Save();
            configFile.SaveOnConfigSet = true;
        }

        private string MakeMonsterString()
        {
            string str = "";

            str += "flowerman:180,";
            str += "crawler:90,";
            str += "hoarding bug:2,";
            str += "centipede:10,";
            str += "radmech:100,";
            str += "bunker spider:50,";
            str += "puffer:10,";
            str += "jester:250,";
            str += "blob:2,";
            str += "girl:180,";
            str += "spring:30,";
            str += "nutcracker:80,";
            str += "masked:60,";
            str += "mouthdog:40,";
            str += "earth leviathan:120,";
            str += "forestgiant:40,";
            str += "baboon hawk:10,";
            str += "red locust bees:2,";
            str += "docile locust bees:0,";
            str += "manticoil:0,";
            str += "peeper:2,";
            str += "locker:60,";
            str += "fiend:80";

            return str;
        }
    }
}
