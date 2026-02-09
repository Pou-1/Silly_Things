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

        public Config(ConfigFile configFile)
        {
            MorphingCaseItemRarity = configFile.Bind(
                "Spawn Rates",
                "MorphingCaseItemRarity",
                10,
                "Rarity of the Morphing Case Item (higher = more common)."
            );
        }
    }
}
