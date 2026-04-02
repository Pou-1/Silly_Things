
using GameNetcodeStuff;
using Silly_Things.codes.CameraItem;
using System;
using System.Collections.Generic;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

namespace Silly_Things.codes.BountyContract
{
    public class HelperBountyContract
    {
        public static List<MonsterNameBounty> MonsterValues = new List<MonsterNameBounty>();

        // _____________MONSTER VALUE_____________ \\
        public struct MonsterNameBounty
        {
            public string Name;
            public int Value;
            public int ItemCount;

            public MonsterNameBounty(string n, int v, int c)
            {
                Name = n;
                Value = v;
                ItemCount = c;
            }
        }

        public static (List<EnemyAI>, List<PlayerControllerB>, int, int) AssignTarget(PlayerControllerB playerHeld)
        {
            List<EnemyAI> possibleEnemies = new List<EnemyAI>();
            List<PlayerControllerB> possiblePlayers = new List<PlayerControllerB>();
            int itemCount = 3;
            Plugin.Logger.LogError("AssignTarget ");

            foreach (EnemyAI enemyInGame in EnemyAI.FindObjectsOfType<EnemyAI>())
            {
                if (enemyInGame != null && !enemyInGame.isEnemyDead)
                {
                    var monster = MonsterValues.Find(m => m.Name == enemyInGame.enemyType.enemyName.Trim().ToLower());
                    Plugin.Logger.LogError(monster);
                    if (monster.Name != null)
                    {
                        possibleEnemies.Add(enemyInGame);
                        Plugin.Logger.LogError(enemyInGame + " added");
                    }
                }
            }

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player != null && player.isPlayerControlled && player != playerHeld && !player.isPlayerDead)
                {
                    possiblePlayers.Add(player);
                    Plugin.Logger.LogError(player + " added");
                }
            }

            int playerChance = Plugin.SillyThingsConfig.BountyChanceToFocusPlayer.Value;
            bool tryPlayer = UnityEngine.Random.Range(0, 100) < playerChance;

            if (tryPlayer && possiblePlayers.Count > 0)
            {
                PlayerControllerB targetPlayer = possiblePlayers[UnityEngine.Random.Range(0, possiblePlayers.Count)];
                Plugin.Logger.LogError(targetPlayer + " target");
                possibleEnemies.Clear();
                possiblePlayers.Clear();
                possiblePlayers.Add(targetPlayer);
                Plugin.Logger.LogError(Plugin.SillyThingsConfig.BountyRewardForKillingPlayer.Value + " target");
                Plugin.Logger.LogError(itemCount + " target");
                return (possibleEnemies, possiblePlayers, Plugin.SillyThingsConfig.BountyRewardForKillingPlayer.Value, itemCount);
            }

            if (possibleEnemies.Count > 0)
            {
                EnemyAI targetEnemy = possibleEnemies[UnityEngine.Random.Range(0, possibleEnemies.Count)];
                string enemyName = targetEnemy.enemyType.enemyName.Trim().ToUpper();
                var monster = HelperBountyContract.MonsterValues.Find(m => m.Name == targetEnemy.enemyType.enemyName.Trim().ToLower());
                int reward = 0;

                if (monster.Name != null)
                {
                    reward = monster.Value;
                    itemCount = monster.ItemCount;
                }
                Plugin.Logger.LogError(targetEnemy + " target");
                Plugin.Logger.LogError(reward + " target");
                Plugin.Logger.LogError(itemCount + " target");

                possiblePlayers.Clear();
                possibleEnemies.Clear();
                possibleEnemies.Add(targetEnemy);

                return (possibleEnemies, possiblePlayers, reward, itemCount);
            }
            possiblePlayers.Clear();
            possibleEnemies.Clear();

            return (possibleEnemies, possiblePlayers, 0, 0);
        }
    }
}
