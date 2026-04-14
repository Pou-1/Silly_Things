using BepInEx.Configuration;

namespace Silly_Things
{
    public class Config
    {
        public ConfigEntry<bool> debugMode;
        public ConfigEntry<bool> iconCustom;

        // _____________MORPHING CASE_____________ \\
        public ConfigEntry<int> MorphingCaseItemRarity;

        // _____________SNAKE CARDBOARD BOX_____________ \\
        public ConfigEntry<int> SnakeCardboardBox;

        // _____________BOUNTY_____________ \\
        public ConfigEntry<int> BountyContract;
        public ConfigEntry<int> BountyChanceToFocusPlayer;
        public ConfigEntry<int> BountyRewardForKillingPlayer;
        public ConfigEntry<string> BountymonsterValues;

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
        public ConfigEntry<bool> cameraHasBattery;
        public ConfigEntry<int> cameraBatteryNumberOfPickBeforeZero;
        public ConfigEntry<int> enemyDetectionDistance;

        // _____________CAMERA MOB_____________ \\
        public ConfigEntry<string> monsterValues;
        public ConfigEntry<int> defaultMonsterValue;
        public ConfigEntry<int> monsterValueMultiplier;
        public ConfigEntry<bool> monsterReactToFlash;

        // _____________FLASH_____________ \\
        public ConfigEntry<float> flashAngle;
        public ConfigEntry<float> flashIntensity;
        public ConfigEntry<float> flashRange;

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
                "General",
                "debugMode",
                false,
                "More log"
            );

            iconCustom = configFile.Bind(
               "General",
               "IconCustom",
               true,
               "Whether or not you want custom icons"
           );

            MorphingCaseItemRarity = configFile.Bind(
                "Morphing Case",
                "MorphingCaseItemRarity",
                18,
                "Rarity of the Morphing Case Item (higher = more common)."
            );

            SnakeCardboardBox = configFile.Bind(
                "Snake Cardboard Box",
                "SnakeCardboardBoxItemRarity",
                10,
                "Rarity of the Snake Card Box Item (higher = more common)."
            );

            // _____________BOUNTY_____________ \\
            BountyContract = configFile.Bind(
                "Bounty Contract",
                "BountyContractItemRarity",
                15,
                "Rarity of the Bounty Contract Item (higher = more common)."
            );

            BountyChanceToFocusPlayer = configFile.Bind(
                "Bounty Contract",
                "BountyChanceToFocusPlayer",
                10,
                "Percentage of chance for a bounty contract to focus a player (higher = more chance)."
            );

            BountymonsterValues = configFile.Bind(
               "Bounty Contract",
               "Monsters Bounty value",
               "" + MakeMonsterBountyString(),
               "Bounty by monster Assign a monster name to a value monsterName1:scrapValue1,monsterName2:scrapValue2,... (Can be used to overide base monster values too)"
           );

            BountyRewardForKillingPlayer = configFile.Bind(
                "Bounty Contract",
                "BountyRewardForKillingPlayer",
                50,
                "Amount of Money earn when a player is killed in a bounty contract (higher = more money)."
            );

            // _____________CAMERA_____________ \\
            DeletePictureOnLaunch = configFile.Bind(
                "Camera",
                "DeletePictureOnLaunch",
                false,
                "Are all the picture getting deleted on every launch of lethal company"
            );

            monsterValues = configFile.Bind(
                "Camera",
                "Monsters value",
                "" + MakeMonsterString(),
                "Assign a monster name to a value monsterName1:scrapValue1,monsterName2:scrapValue2,... (Can be used to overide base monster values too)"
            );

            defaultMonsterValue = configFile.Bind(
                "Camera",
                "Default monster value",
                10,
                "Change default value for monster not listed above (MonsterValues)"
            );

            enemyDetectionDistance = configFile.Bind(
                "Camera",
                "CameraDetectDist",
                30,
                "The depth where the camera detect an enemy or player"
            );

            monsterValueMultiplier = configFile.Bind(
                "Camera",
                "Monster value multiplier",
                1,
                "Change this value to multiply the value of all taken monster"
            );

            monsterReactToFlash = configFile.Bind(
                "Camera",
                "Monster React to flash",
                true,
                "Does monster have special reaction when taken in picture"
            );

            cameraLootRarity = configFile.Bind(
                "Camera",
                "CameraLootRarity",
                20,
                "Chance of looting a camera (1 very rare, 100 very common)"
            );

            cameraHasBattery = configFile.Bind(
                "Camera",
                "CameraHasBattery",
                false,
                "Does the camera has a battery ?"
            );

            cameraBatteryNumberOfPickBeforeZero = configFile.Bind(
                "Camera",
                "cameraBatteryNumberOfPickBeforeZero",
                10,
                "if the camera has battery, number of pics with a full charge"
            );

            cameraCanBeBuy = configFile.Bind(
                "Camera",
                "CameraCanBeBuy",
                true,
                "Does the camera appear in the store ?"
            );

            cameraCost = configFile.Bind(
                "Camera",
                "CameraCost",
                150,
                "The cost of the camera in the store"
            );

            cameraFov = configFile.Bind(
                "Camera",
                "CameraFov",
                75f,
                "The Field of view of the camera"
            );

            flashAngle = configFile.Bind(
                "Camera",
                "FlashAngle",
                160f,
                "The wideness of the flash (in degree)"
            );

            flashIntensity = configFile.Bind(
                "Camera",
                "FlashIntensity",
                250f,
                "The intensity of the flash"
            );

            flashRange = configFile.Bind(
                "Camera",
                "FlashRange",
                50f,
                "The range of the flash in meter"
            );

            pictureResolutionWidth = configFile.Bind(
                "Camera",
                "PictureResolutionWidth",
                1920,
                "The width resolution of the picture taken in your pc (careful can heavily impact performance when the camera is held)"
            );

            pictureResolutionHeight = configFile.Bind(
               "Camera",
               "pictureResolutionHeight",
               1080,
               "The height resolution of the picture taken in your pc (careful can heavily impact performance when the camera is held)"
            );

            pictureResolutionDepth = configFile.Bind(
                "Camera",
                "pictureResolutionDepth",
                50,
                "The depth resolution of the picture taken in your pc"
            );

            screenResolutionWidth = configFile.Bind(
                "Camera",
                "screenResolutionWidth",
                640,
                "The width resolution of the camera screen (careful can heavily impact performance when the camera is held)"
            );

            screenResolutionHeight = configFile.Bind(
                "Camera",
                "screenResolutionHeight",
                360,
                "The height resolution of the camera screen (careful can heavily impact performance when the camera is held)"
            );

            screenResolutionDepth = configFile.Bind(
                "Camera",
                "screenResolutionDepth",
                50,
                "The depth of the camera screen"
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

            str += "flowerman:150,";
            str += "crawler:30,";
            str += "hoarding bug:10,";
            str += "centipede:10,";
            str += "radmech:150,";
            str += "bunker spider:30,";
            str += "puffer:10,";
            str += "jester:300,";
            str += "hygrodere:10,";
            str += "ghost girl:30,";
            str += "spring:30,";
            str += "nutcracker:100,";
            str += "masked:10,";
            str += "mouthdog:100,";
            str += "earth leviathan:120,";
            str += "forestgiant:80,";
            str += "baboon hawk:10,";
            str += "red locust bees:2,";
            str += "docile locust bees:0,";
            str += "manticoil:0,";
            str += "peeper:2,";
            str += "maneater:200,";
            str += "locker:30,";
            str += "fiend:80,";
            str += "barber:150,";
            str += "giant sapsucker:150,";
            str += "butler:80,";
            str += "kidnapper fox:150,";
            str += "lasso man:30,";
            str += "cadaver bloom:100,";
            str += "feiopar:150,";
            str += "backwater gunkfish:20";

            return str;
        }

        private string MakeMonsterBountyString()
        {
            string str = "";

            str += "flowerman:300:5,";
            str += "crawler:75:2,";
            str += "hoarding bug:50:1,";
            str += "centipede:75:2,";
            str += "bunker spider:150:5,";
            str += "butler:250:5,";
            str += "maneater:500:10,";
            str += "masked:50:1,";
            str += "nutcracker:250:5,";
            str += "baboon hawk:100:3,";
            str += "mouthdog:300:6,";
            str += "forestgiant:200:5,";
            str += "kidnapper fox:250:6,";

            return str;
        }
    }
}
