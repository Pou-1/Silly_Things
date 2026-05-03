using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Silly_Things.codes.CameraItem
{
    internal class HelperCameraEnemy
    {
        public static List<MonsterNameValue> additionalMonsterValues = new List<MonsterNameValue>();

        // _____________MONSTER VALUE_____________ \\
        public struct MonsterNameValue
        {
            public string Name;
            public int Value;

            public MonsterNameValue(string n, int v)
            {
                Name = n;
                Value = v;
            }
        }

        // _____________DETECTION_____________ \\
        public static (List<EnemyAI>, List<PlayerControllerB>) GetVisibleEntities(PlayerControllerB owner, List<EnemyAI> MonsterIntoPicture, List<PlayerControllerB> PlayerIntoPicture, Camera camera)
        {
            MonsterIntoPicture.Clear();
            PlayerIntoPicture.Clear();
            LayerMask layerMask = new LayerMask();
            PrintLayerMask(layerMask, "Raycast layer");
            float detectionDistance = Plugin.SillyThingsConfig.enemyDetectionDistance.Value;

            PlayerControllerB[] players = MonoBehaviour.FindObjectsOfType<PlayerControllerB>(false);
            Vector3[] playersPoint = new Vector3[]
            {
                new Vector3(0, 2.25f, 0),
                new Vector3(0, 2f, 0),
                new Vector3(0, 1.75f, 0),
                new Vector3(0, 1.5f, 0),
                new Vector3(0, 1.25f, 0),
                new Vector3(0, 1f, 0),
                new Vector3(0, 0.75f, 0),
                new Vector3(0, 0.5f, 0),
                new Vector3(0, 0.25f, 0),
                new Vector3(0, 0, 0),
            };

            foreach (PlayerControllerB player in players)
            {
                if (player.isFreeCamera)
                    continue;
                if (!player.isPlayerControlled)
                    continue;

                foreach (Vector3 point in playersPoint)
                {
                    Vector3 p = player.transform.position + point;

                    if (IsInViewPort(p, camera))
                    {
                        float distance = Vector3.Distance(p, camera.transform.position);
                        if (IsInRange(p, distance, detectionDistance))
                        {
                            if (IsRayCastVisible(p, player.gameObject, camera, layerMask))
                            {
                                PlayerIntoPicture.Add(player);
                                break;
                            }
                        }
                    }
                }
            }

            EnemyAI[] enemies = MonoBehaviour.FindObjectsOfType<EnemyAI>(false);
            foreach (EnemyAI enemy in enemies)
            {
                foreach (Vector3 point in playersPoint)
                {
                    Vector3 p = enemy.transform.position + point;

                    if (IsInViewPort(p, camera))
                    {
                        float distance = Vector3.Distance(p, camera.transform.position);
                        if (IsInRange(p, distance, detectionDistance))
                        {
                            if (IsRayCastVisible(p, enemy.gameObject, camera, layerMask))
                            {
                                var earthWorm = enemy as SandWormAI;
                                if (earthWorm != null && !earthWorm.emerged)
                                {
                                    break;
                                }

                                MonsterIntoPicture.Add(enemy);

                                if (Plugin.SillyThingsConfig.monsterReactToFlash.Value)
                                {
                                    if (owner)
                                    {
                                        ReactToFlash(owner, enemy, distance, true);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            return (MonsterIntoPicture, PlayerIntoPicture);
        }

        public static void PrintLayerMask(LayerMask layerMask, string layerMaskName)
        {
            Helper.Helper.LogDebugMod("||||| printing layers of layermask " + layerMaskName, "");
            for (int i = 0; i < 31; i++)
            {
                if (IsInLayerMask(i, layerMask))
                {
                    Helper.Helper.LogDebugMod("-> " + i + " " + LayerMask.LayerToName(i), "");
                }
            }
            Helper.Helper.LogDebugMod("||||| End of : " + layerMaskName, "");
        }

        public static bool IsInLayerMask(int layer, LayerMask layermask)
        {
            return layermask == (layermask | (1 << layer));
        }

        public static bool IsInRange(Vector3 p, float distance, float detectionDistance)
        {
            if (distance > detectionDistance)
            {
                return false;
            }
            return true;
        }

        public static bool IsInViewPort(Vector3 p, Camera camera)
        {
            Vector3 vwp = camera.WorldToViewportPoint(p);
            if (vwp.z > 0 && vwp.x > 0 && vwp.x < 1 && vwp.y > 0 && vwp.y < 1)
            {
                return true;
            }
            return false;
        }

        public static bool IsRayCastVisible(Vector3 p, GameObject target, Camera camera, LayerMask layerMask)
        {
            RaycastHit hit;
            float d = Vector3.Distance(camera.transform.position, p);
            if (Physics.Raycast(camera.transform.position, p - camera.transform.position, out hit, d, layerMask))
            {
                if (hit.transform.gameObject == target)
                {
                    if (Plugin.DebugMode)
                    {
                        Plugin.Logger.LogWarning("Hit target");
                        SpawnDebugVisual(hit.point, Color.green);
                    }
                    return true;
                }

                if (Plugin.DebugMode)
                {
                    Plugin.Logger.LogWarning("SOMETHING IS BLOCKING : " + hit.transform.name + "  " + LayerMask.LayerToName(hit.transform.gameObject.layer));
                    SpawnDebugVisual(hit.point, Color.red);
                }
                return false;

            }
            if (Plugin.DebugMode)
            {
                SpawnDebugVisual(p, Color.green);
            }
            return true;
        }

        public static GameObject SpawnDebugVisual(Vector3 pos, Color c)
        {
            GameObject g = MonoBehaviour.FindObjectOfType<UnlockableSuit>().gameObject;
            GameObject v = g.transform.Find("SuitHook").gameObject;
            GameObject n = MonoBehaviour.Instantiate(v, pos, Quaternion.identity, null);

            Material mat = n.GetComponent<MeshRenderer>().material;
            mat.color = c;
            mat.SetColor("_EmissiveColor", c);
            mat.mainTexture = null;
            mat.SetTexture("_EmissiveColorMap", null);

            return n;
        }

        // _____________SCORE_____________ \\
        public static void LoadMonstersValues()
        {
            string monsters = Plugin.SillyThingsConfig.monsterValues.Value;
            string[] monsterValuePair = monsters.Split(",");

            Helper.Helper.LogDebugMod("Display monsters and there values : ", "");
            foreach (string mvp in monsterValuePair)
            {
                string[] m = mvp.Split(":");
                if (m.Length == 2)
                {
                    try
                    {
                        int value = Int32.Parse(m[1]);
                        var p = new MonsterNameValue(m[0].ToLower(), value);
                        additionalMonsterValues.Add(p);
                        Helper.Helper.LogDebugMod("--> " + p.Name + "  " + p.Value, "");
                    }
                    catch (FormatException)
                    {
                        Plugin.Logger.LogError("Add monster config error! Scrap value isn't a number! ");
                    }
                }
                else
                {
                    Plugin.Logger.LogError("Error in config files ! Can't read entry: " + mvp + " (don't add \'|\' at the end)");
                }
            }
        }


        public static float GetMonstersScore(List<EnemyAI> monsters, HashSet<ulong> photographedEnemies)
        {
            Helper.Helper.LogDebugMod("GetMonstersScore", "");

            float total = 0f;

            foreach (EnemyAI enemy in monsters)
            {
                if (enemy != null)
                {
                    if (!photographedEnemies.Contains(enemy.NetworkObjectId))
                    {
                        string monsterName = enemy.enemyType.enemyName;
                        total += GetMonsterScore(monsterName);
                    }
                }
            }

            return total;
        }

        public static float GetMonsterScore(string monsterName)
        {
            Helper.Helper.LogDebugMod("GetMonstersScore", "");

            string name = monsterName.ToLower();
            float value = Plugin.SillyThingsConfig.defaultMonsterValue.Value;

            foreach (MonsterNameValue m in additionalMonsterValues)
            {
                if (name == m.Name)
                {
                    value = m.Value;
                    break;
                }
            }

            return value;
        }

        public static string GetEntitiesNames(List<EnemyAI> MonsterIntoPicture, List<PlayerControllerB> PlayerIntoPicture)
        {
            Helper.Helper.LogDebugMod("GetEntitiesNames", "");

            string Names = "";

            if (MonsterIntoPicture.Count > 0 && PlayerIntoPicture.Count > 0)
                return Names;

            Dictionary<string, int> enemyCounts = new Dictionary<string, int>();

            foreach (EnemyAI enemy in MonsterIntoPicture)
            {
                if (enemy != null && enemy.NetworkObject != null)
                {
                    string name = enemy.enemyType.enemyName;

                    if (enemyCounts.ContainsKey(name))
                        enemyCounts[name]++;
                    else
                        enemyCounts[name] = 1;
                }
            }

            foreach (var kvp in enemyCounts)
            {
                if (kvp.Value > 1)
                    Names += $"{kvp.Value} {kvp.Key}, ";
                else
                    Names += $"{kvp.Key}, ";
            }

            foreach (PlayerControllerB player in PlayerIntoPicture)
            {
                if (player != null && player.isPlayerControlled)
                {
                    Names += player.playerUsername + ", ";
                }
            }

            return Names;
        }

        // _____________REACT TO FLASH_____________ \\
        public static void ReactToFlash(PlayerControllerB owner, EnemyAI enemy, float distance, bool isInPicture)
        {
            if (!owner || !enemy)
            {
                Plugin.Logger.LogError("No owner or enemy ! Enemy can't react");
                return;
            }

            enemy.DetectNoise(owner.transform.position, 1.5f, 1, 0);

            string lowerMonsterName = enemy.enemyType.enemyName.ToLower();
            if (enemy is HoarderBugAI)
            {
                StunMonster(enemy, owner, 0.5f);
            }
            else if (enemy is FlowermanAI flowerman)
            {
                if (ChanceOfTrigger(owner.playerUsername, 0.5f))
                {
                    flowerman.SwitchToBehaviourStateOnLocalClient(2);
                    flowerman.EnterAngerModeServerRpc(100f);
                }
            }
            else if (enemy is BaboonBirdAI baboon)
            {
                baboon.SwitchToBehaviourStateOnLocalClient(2);
            }
            else if (enemy is RadMechAI mech)
            {
                if (ChanceOfTrigger(owner.playerUsername, 0.5f))
                    mech.SwitchToBehaviourStateOnLocalClient(2);
            }
            else if (enemy is JesterAI jester)
            {
                if (ChanceOfTrigger(owner.playerUsername, 0.33f))
                {
                    jester.beginCrankingTimer = 0;
                    jester.popUpTimer = 0;
                }
            }
            else if (enemy is SpringManAI)
            {
                StunMonster(enemy, owner, 5f);
            }
            else if (enemy is CaveDwellerAI maneater)
            {
                maneater.ScareBabyServerRpc();
            }
            else if (enemy is PumaAI feiopar)
            {
                if (ChanceOfTrigger(owner.playerUsername, 0.33f))
                    feiopar.SwitchToBehaviourStateOnLocalClient(2);
            }
            else if (enemy is StingrayAI stingray)
            {
                if (ChanceOfTrigger(owner.playerUsername, 0.5f))
                    stingray.SwitchToBehaviourStateOnLocalClient(0);
            }
            else if (enemy is BushWolfEnemy fox)
            {
                if (ChanceOfTrigger(owner.playerUsername, 0.5f))
                    fox.SwitchToBehaviourStateOnLocalClient(3);
            }
            else if (enemy is GiantKiwiAI giantbird)
            {
                giantbird.AddToThreatsHoldingEggListServerRpc((int)owner.playerClientId);
            }
            else
            {
                TargetOwner(enemy, owner);
            }
            Plugin.Logger.LogError("Picture of " + lowerMonsterName + " added");
        }

        private static void StunMonster(EnemyAI enemy, PlayerControllerB owner, float stunDuration)
        {
            Plugin.Logger.LogInfo(enemy.enemyType.name + " is stunned by " + owner.playerUsername);
            enemy.SetEnemyStunned(true, stunDuration, owner);
        }

        private static void TargetOwner(EnemyAI enemy, PlayerControllerB owner)
        {
            Plugin.Logger.LogInfo(enemy.enemyType.name + " target " + owner.playerUsername);
            enemy.SetMovingTowardsTargetPlayer(owner);
        }

        public static bool ChanceOfTrigger(string ownerName, float chance)
        {
            if (Plugin.SillyThingsConfig.cursedPlayers.Value.Contains(ownerName))
                return true;

            if (UnityEngine.Random.value <= chance)
            {
                return true;
            }
            Plugin.Logger.LogWarning("Lucky! the monster didn't react to the flash (this time)");
            return false;
        }
    }
}
