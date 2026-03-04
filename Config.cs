using BepInEx.Configuration;

namespace Silly_Things
{
    internal class Config
    {
        public ConfigEntry<int> MorphingCaseItemRarity
        {
            get; private set;
        }

        public ConfigEntry<int> SnakeCardboardBox
        {
            get; private set;
        }

        public ConfigEntry<int> BountyContract
        {
            get; private set;
        }

        public ConfigEntry<int> BountyChanceToFocusPlayer
        {
            get; private set;
        }

        public ConfigEntry<int> BountyRewardForKillingPlayer
        {
            get; private set;
        }

        public Config(ConfigFile configFile)
        {
            configFile.SaveOnConfigSet = false;
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
                10,
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

            configFile.Save();
            configFile.SaveOnConfigSet = true;
        }
    }
}
