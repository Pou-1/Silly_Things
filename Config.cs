using BepInEx.Configuration;

namespace Silly_Things
{
    internal class Config
    {
        public ConfigEntry<bool> EnableDebugMode
        {
            get; private set;
        }

        public ConfigEntry<int> MorphingCaseItemRarity
        {
            get; private set;
        }

        public ConfigEntry<int> SnakeCardboardBox
        {
            get; private set;
        }

        public Config(ConfigFile configFile)
        {
            MorphingCaseItemRarity = configFile.Bind(
                "Spawn Rates",
                "MorphingCaseItemRarity",
                10,
                "Rarity of the Morphing Case Item (higher = more common)."
            );

            /*SnakeCardboardBox = configFile.Bind(
                "Spawn Rates",
                "SnakeCardboardBoxItemRarity",
                10,
                "Rarity of the Snake Card Box Item (higher = more common)."
            );*/

            configFile.Save();
            configFile.SaveOnConfigSet = true;
        }
    }
}
