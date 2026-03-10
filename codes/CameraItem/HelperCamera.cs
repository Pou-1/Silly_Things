using GameNetcodeStuff;
using Silly_Things.codes.Helper;
using System.Collections.Generic;
using UnityEngine;

namespace Silly_Things.codes.CameraItem
{
    internal class HelperCamera
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
            float detectionDistance = 50;

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
        public static float GetMonstersScore(List<EnemyAI> monsters, List<EnemyAI> photographedEnemies)
        {
            Helper.Helper.LogDebugMod("GetMonstersScore", "");

            float total = 0f;

            foreach (EnemyAI enemy in monsters)
            {
                if (enemy != null)
                {
                    if (!photographedEnemies.Contains(enemy))
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

            foreach (EnemyAI enemy in MonsterIntoPicture)
            {
                if (enemy != null && enemy.NetworkObject != null)
                {
                    Names += " " + enemy.enemyType.enemyName;
                }
            }

            foreach (PlayerControllerB player in PlayerIntoPicture)
            {
                if (player != null && player.isPlayerControlled)
                {
                    Names += " " + player.playerUsername;
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
            switch (lowerMonsterName)
            {
                case "flowerman":
                    if (ChanceOfTrigger(0.5f))
                        AngryFlowerman(enemy, owner);
                    break;
                case "crawler":
                    TargetOwner(enemy, owner);
                    break;
                case "hoarding bug":
                    StunMonster(enemy, owner, 10f);
                    break;
                case "centipede":
                    TargetOwner(enemy, owner);
                    break;
                case "bunker spider":
                    TargetOwner(enemy, owner);
                    break;
                case "puffer":
                    TargetOwner(enemy, owner);
                    break;
                case "jester":
                    if (ChanceOfTrigger(0.33f))
                        AngryJester(enemy, owner);
                    break;
                case "blob":
                    TargetOwner(enemy, owner);
                    break;
                case "girl":
                    TargetOwner(enemy, owner);
                    break;
                case "spring":
                    StunMonster(enemy, owner, 10f);
                    break;
                case "nutcracker":
                    TargetOwner(enemy, owner);
                    break;
                case "masked":
                    TargetOwner(enemy, owner);
                    break;
                case "mouthdog":
                    TargetOwner(enemy, owner);
                    break;
                case "earth leviathan":
                    TargetOwner(enemy, owner);
                    break;
                case "forestgiant":
                    TargetOwner(enemy, owner);
                    break;
                case "baboon hawk":
                    TargetOwner(enemy, owner);
                    break;
                case "red locust bees":
                    TargetOwner(enemy, owner);
                    break;
                case "docile locust bees":
                    TargetOwner(enemy, owner);
                    break;
                case "manticoil":
                    TargetOwner(enemy, owner);
                    break;
                default:
                    Plugin.Logger.LogError("No special behaviour for " + lowerMonsterName);
                    break;
            }
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

        private static void AngryJester(EnemyAI enemy, PlayerControllerB owner)
        {
            JesterAI? jester = enemy as JesterAI;
            if (jester == null)
                return;

            Plugin.Logger.LogInfo("Jester pop !");
            jester.beginCrankingTimer = 0;
            jester.popUpTimer = 0;
        }

        private static void AngryFlowerman(EnemyAI enemy, PlayerControllerB owner)
        {
            FlowermanAI? flowerman = enemy as FlowermanAI;
            if (flowerman == null)
                return;

            flowerman.SwitchToBehaviourStateOnLocalClient(2);
            flowerman.EnterAngerModeServerRpc(100f);
        }

        public static bool ChanceOfTrigger(float chance)
        {
            if (UnityEngine.Random.value <= chance)
            {
                return true;
            }
            Plugin.Logger.LogWarning("Lucky! the monster didn't react to the flash (this time)");
            return false;
        }
    }
}
