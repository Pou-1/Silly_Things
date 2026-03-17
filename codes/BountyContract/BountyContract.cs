using GameNetcodeStuff;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Silly_Things.codes.BountyContract
{
    public class BountyContract : PhysicsProp
    {
        private AudioSource? audio;
        private bool isBountyComplete = false;

        // _____________TARGET_____________ \\
        public PlayerControllerB? targetPlayer;
        public EnemyAI? targetEnemy;
        public bool targetAssigned = false; 
        public bool isPlayerTarget = false;
        private Coroutine? searchTargetCoroutine;

        // _____________UI_____________ \\
        private GameObject? uiInstance;
        private TMP_Text? targetName; 
        private TMP_Text? rewardText; 
        private GameObject? bountyCompletePanel;

        // _____________REWARDS_____________ \\
        private int reward = 0;
        private List<Item> itemsInRewards = new List<Item>();
        private List<int> rewardsPrice = new List<int>();

        // _____________FOOTPRINTS_____________ \\
        private Coroutine? trackingCoroutine; 
        private Queue<Vector3> footprintQueue = new Queue<Vector3>(); 
        public GameObject? footprintPrefab; 
        private bool generatingPath = false;
        private float nextPathTime = 0f;


        // _____________JSON_____________ \\
        public static List<EnemyWeight>? _reader{get; set;}
        public static Dictionary<string, int> WeightsByType{get; set;} =new Dictionary<string, int>();
        public static Dictionary<string, int> ItemsCountByType { get; set; } = new Dictionary<string, int>();
        public class EnemyWeight
        {
            public string? enemyName;
            public int reward;
            public int numberOfItemInReward;
        }

        // _____________OVERRIDE_____________ \\
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            footprintPrefab = Plugin.Instance.FootprintPrefab;
            audio = transform.Find("Audio").GetComponent<AudioSource>();
        }

        public override void EquipItem()
        {
            base.EquipItem();

            if (!IsOwner)
            {
                return;
            }

            if (uiInstance == null)
            {
                uiInstance = Instantiate(Plugin.Instance.UI_Bounty);
            }

            if (uiInstance != null)
            {
                targetName = uiInstance.transform.Find("Panel/MobName")?.GetComponent<TMP_Text>();
                rewardText = uiInstance.transform.Find("Panel/Rewards")?.GetComponent<TMP_Text>();
                bountyCompletePanel = uiInstance.transform.Find("BountyComplete")?.gameObject;
                uiInstance.SetActive(true);

                if (!isBountyComplete)
                    bountyCompletePanel?.SetActive(false);
            }

            if (!isBountyComplete && !StartOfRound.Instance.inShipPhase)
            {
                if (!targetAssigned)
                {
                    searchTargetCoroutine = StartCoroutine(SearchTargetLoop());
                }

                if (targetAssigned)
                {
                    trackingCoroutine = StartCoroutine(TrackingLoop());
                }
            }
        }

        public override void PocketItem()
        {
            base.PocketItem();
            if (trackingCoroutine != null)
            {
                StopCoroutine(trackingCoroutine);
            }
            if (searchTargetCoroutine != null)
            {
                StopCoroutine(searchTargetCoroutine);
            }
            uiInstance?.SetActive(false);
        }

        public override void DiscardItem()
        {
            base.DiscardItem();

            if (uiInstance != null)
            {
                Destroy(uiInstance);
                uiInstance = null;
                targetName = null;
                rewardText = null;
                bountyCompletePanel = null;
            }

            if (trackingCoroutine != null)
            {
                StopCoroutine(trackingCoroutine);
                trackingCoroutine = null;
            }

            if (searchTargetCoroutine != null)
            {
                StopCoroutine(searchTargetCoroutine);
                searchTargetCoroutine = null;
            }
        }

        // _____________COROUTINE_____________ \\
        private IEnumerator SearchTargetLoop()
        {
            while (!isBountyComplete && !targetAssigned)
            {
                AssignTargetLocal();

                if (targetAssigned)
                {
                    trackingCoroutine = StartCoroutine(TrackingLoop());
                    searchTargetCoroutine = null;
                    yield break;
                }

                yield return new WaitForSeconds(0.25f);
            }
        }

        private IEnumerator TrackingLoop()
        {
            while (!isBountyComplete)
            {
                if (targetAssigned)
                {
                    if (searchTargetCoroutine != null)
                    {
                        StopCoroutine(searchTargetCoroutine);
                    }

                    if (IsTargetDead())
                    {
                        //Plugin.Logger.LogInfo("[Bounty] Target dead");
                        OnTargetCompleted();
                        isBountyComplete = true;
                        yield break;
                    }

                    ShowPathToTarget();
                    UpdateEMF();
                    UpdateUI();
                }

                yield return new WaitForSeconds(0.05f);
            }
        }

        private IEnumerator SpawnFootprintsOverTime()
        {
            generatingPath = true;

            Vector3? previous = null;
            bool leftFoot = true;

            while (footprintQueue.Count > 0)
            {
                Vector3 current = footprintQueue.Dequeue();

                Vector3 direction;
                if (previous.HasValue)
                    direction = (current - previous.Value).normalized;
                else if (footprintQueue.Count > 0)
                    direction = (footprintQueue.Peek() - current).normalized;
                else
                    direction = playerHeldBy.transform.forward;

                Vector3 offset = Vector3.zero;
                Vector3 perp = Vector3.Cross(Vector3.up, direction).normalized;
                offset = perp * (leftFoot ? -0.2f : 0.2f);

                offset *= UnityEngine.Random.Range(0.9f, 1.1f);

                SpawnFootprint(current + offset, direction);

                previous = current;
                leftFoot = !leftFoot;

                yield return new WaitForSeconds(0.1f);
            }

            generatingPath = false;
        }

        public IEnumerator SyncItem(NetworkObjectReference reference, int intValue)
        {
            EnableItemMeshes(enable: false);
            NetworkObject? itemNetObject = null;
            float startTime = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - startTime < 8f && !reference.TryGet(out itemNetObject))
            {
                yield return new WaitForSeconds(0.1f);
            }

            if (itemNetObject == null)
            {
                yield break;
            }

            yield return new WaitForEndOfFrame();
            GrabbableObject component = itemNetObject.GetComponent<GrabbableObject>();
            component.fallTime = 0f;

            if (component.itemProperties.isScrap)
                component.SetScrapValue(intValue);
        }

        // _____________TARGET_____________ \\
        private bool IsTargetDead()
        {
            if (isPlayerTarget)
            {
                return targetPlayer == null || targetPlayer.isPlayerDead;
            }
            else
            {
                return targetEnemy == null || targetEnemy.isEnemyDead;
            }
        }

        public void AssignTargetLocal()
        {
            if (!IsOwner)
                return;

            List<EnemyAI> possibleEnemies = new List<EnemyAI>();
            List<PlayerControllerB> possiblePlayers = new List<PlayerControllerB>();

            foreach (EnemyAI enemyInGame in FindObjectsOfType<EnemyAI>())
            {
                if (enemyInGame != null && !enemyInGame.isEnemyDead)
                {
                    var monster = HelperBountyContract.MonsterValues.Find(m => m.Name == enemyInGame.enemyType.enemyName.Trim().ToLower());
                    if (monster.Name != null)
                    {
                        possibleEnemies.Add(enemyInGame);
                    }
                        possibleEnemies.Add(enemyInGame);
                }
            }

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player != null && player.isPlayerControlled && player != playerHeldBy && !player.isPlayerDead)
                    possiblePlayers.Add(player);
            }

            int playerChance = Plugin.SillyThingsConfig.BountyChanceToFocusPlayer.Value;
            bool tryPlayer = UnityEngine.Random.Range(0, 100) < playerChance;

            if (tryPlayer && possiblePlayers.Count > 0)
            {
                targetPlayer = possiblePlayers[UnityEngine.Random.Range(0, possiblePlayers.Count)];
                isPlayerTarget = true;
                targetAssigned = true;

                CreateRewardServerRpc(new NetworkObjectReference(targetPlayer.gameObject), true);
                return;
            }

            if (possibleEnemies.Count > 0)
            {
                targetEnemy = possibleEnemies[UnityEngine.Random.Range(0, possibleEnemies.Count)];
                isPlayerTarget = false;

                string enemyName = targetEnemy.enemyType.enemyName.Trim().ToUpper();

                var monster = HelperBountyContract.MonsterValues.Find(m => m.Name == targetEnemy.enemyType.enemyName.Trim().ToLower());
                if (monster.Name != null)
                    reward = monster.Value;

                targetAssigned = true;

                CreateRewardServerRpc(new NetworkObjectReference(targetEnemy.gameObject), false);
            }
        }

        private void OnTargetCompleted()
        {
            HUDManager.Instance.DisplayTip("Bounty complete", $"Reward: ${reward}", true);
            bountyCompletePanel?.SetActive(true);
            SpawnRewardsServerRpc();
        }

        // _____________REWARDS_____________ \\
        public void CreateRewardServer(NetworkObjectReference targetRef, bool isPlayer)
        {
            if (!IsServer)
                return;

            itemsInRewards.Clear();
            rewardsPrice.Clear();

            if (isPlayer)
                targetPlayer = ((GameObject)targetRef).GetComponent<PlayerControllerB>();
            else
                targetEnemy = ((GameObject)targetRef).GetComponent<EnemyAI>();

            int itemCount = 3;
            int rewardTotal = 0;

            if (targetEnemy != null)
            {
                var monster = HelperBountyContract.MonsterValues.Find(m => m.Name == targetEnemy.enemyType.enemyName.Trim().ToLower());
                if (monster.Name != null)
                {
                    reward = monster.Value;
                    itemCount = monster.ItemCount;
                }
            }
            else if (targetPlayer != null)
            {
                rewardTotal = Plugin.SillyThingsConfig.BountyRewardForKillingPlayer.Value;
            }

            List<int> rarityWeights = RoundManager.Instance.currentLevel.spawnableScrap.Select(s => s.spawnableItem.itemName == "Bounty Contract" ? 0 : s.rarity).ToList();

            System.Random rng = new System.Random(UnityEngine.Random.Range(1, 1000000));

            for (int i = 0; i < itemCount; i++)
            {
                int index = RoundManager.Instance.GetRandomWeightedIndexList(rarityWeights, rng);
                itemsInRewards.Add(RoundManager.Instance.currentLevel.spawnableScrap[index].spawnableItem);
            }

            Plugin.Logger.LogError("CreateRewardServer ");
            Plugin.Logger.LogError("rewardTotal " + rewardTotal);
            Plugin.Logger.LogError("items " + itemCount);
            GenerateRewardSplit(rewardTotal, itemCount);
            SyncRewardsClientRpc(rewardTotal, rewardsPrice.ToArray(), targetRef, isPlayer);
        }

        public void GenerateRewardSplit(int rewardTotal, int itemCount)
        {
            rewardsPrice.Clear();

            int remaining = rewardTotal;
            Plugin.Logger.LogError("GenerateRewardSplit" + rewardTotal);

            for (int i = 0; i < itemCount - 1; i++)
            {
                int max = remaining - (itemCount - i - 1);
                int value = UnityEngine.Random.Range(1, max);

                rewardsPrice.Add(value);
                remaining -= value;
                Plugin.Logger.LogError(value);
            }
            rewardsPrice.Add(remaining);
        }

        // _____________REWARDS RPCS ALEDDDDDDD :c_____________ \\
        [ServerRpc(RequireOwnership = false)]
        public void CreateRewardServerRpc(NetworkObjectReference targetRef, bool isPlayer)
        {
            CreateRewardServer(targetRef, isPlayer);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnRewardsServerRpc()
        {
            if (!IsServer)
                return;

            Vector3 positionReward = targetPlayer != null ? targetPlayer.transform.position : targetEnemy != null ? targetEnemy.transform.position : Vector3.zero;

            for (int i = 0; i < itemsInRewards.Count; i++)
            {
                var reference = Helper.Helper.SpawnScrap(itemsInRewards[i], positionReward + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0.4f, UnityEngine.Random.Range(-1f, 1f)), rewardsPrice[i]);

                SpawnRewardsClientRpc(reference.netObjectRef, reference.value);
            }
        }

        [ClientRpc]
        public void SpawnRewardsClientRpc(NetworkObjectReference reference, int intValue)
        {
            StartCoroutine(SyncItem(reference, intValue));
        }

        [ClientRpc]
        public void SyncRewardsClientRpc(int rewardTotal, int[] prices, NetworkObjectReference targetRef, bool isPlayer)
        {
            Plugin.Logger.LogError("SyncRewardsClientRpc" + rewardTotal);
            Plugin.Logger.LogError(prices);

            if (!IsServer)
            {
                if (isPlayer)
                    targetPlayer = ((GameObject)targetRef).GetComponent<PlayerControllerB>();
                else
                    targetEnemy = ((GameObject)targetRef).GetComponent<EnemyAI>();
            }

            reward = rewardTotal;
            rewardsPrice = prices.ToList();
        }

        // :> _____________UI_____________ c: \\
        private void UpdateUI()
        {
            if (uiInstance == null)
                return;

            if (targetName != null)
            {
                if (isPlayerTarget && targetPlayer != null)
                    targetName.text = targetPlayer.playerUsername;

                else if (targetEnemy != null)
                    targetName.text = targetEnemy.enemyType.enemyName;
            }

            if (rewardText != null)
                rewardText.text = "> " + reward + " $";
        }

        // _____________PATH_____________ \\
        public void ShowPathToTarget()
        {
            if (!IsOwner || generatingPath || footprintQueue.Count > 0 || playerHeldBy == null)
                return;

            if (Time.time < nextPathTime)
                return;

            nextPathTime = Time.time + 1.5f;

            Vector3 start = playerHeldBy.transform.position;
            Vector3 end = Vector3.zero;

            if (targetPlayer != null)
                end = targetPlayer.transform.position;

            if (targetEnemy != null)
                end = targetEnemy.transform.position;

            RequestPathServerRpc(start, end);
        }

        public void SpawnFootprint(Vector3 position, Vector3 direction)
        {
            if (footprintPrefab == null)
                return;

            float rotNoise = UnityEngine.Random.Range(-12f, 12f);

            Quaternion rotation =
                Quaternion.LookRotation(direction) *
                Quaternion.Euler(0f, rotNoise, 0f);

            GameObject footprint = Instantiate(footprintPrefab, position, rotation);

            foreach (var col in footprint.GetComponentsInChildren<Collider>())
                col.enabled = false;

            var rb = footprint.GetComponent<Rigidbody>();

            if (rb != null)
                rb.isKinematic = true;

            Destroy(footprint, 2f);
        }

        // _____________PATH RPCS_____________ \\

        [ServerRpc(RequireOwnership = false)]
        public void RequestPathServerRpc(Vector3 start, Vector3 end)
        {
            Queue<Vector3> path = BountyPathSystem.GeneratePathPoints(start, end, 1.5f);
            SpawnPathClientRpc(path.ToArray());
        }

        [ClientRpc]
        public void SpawnPathClientRpc(Vector3[] pathPoints)
        {
            footprintQueue = new Queue<Vector3>(pathPoints);

            if (footprintQueue.Count > 0)
                StartCoroutine(SpawnFootprintsOverTime());
        }

        // _____________EMF_____________ \\
        private float nextBeepTime = 0f;

        public void UpdateEMF()
        {
            if (audio == null || playerHeldBy == null)
                return;

            Vector3 targetPos;

            if (isPlayerTarget)
            {
                if (targetPlayer == null)
                    return;

                targetPos = targetPlayer.transform.position;
            }
            else
            {
                if (targetEnemy == null)
                    return;

                targetPos = targetEnemy.transform.position;
            }

            float dist = Vector3.Distance(playerHeldBy.transform.position, targetPos);

            if (dist > 50f)
                return;

            float interval;

            if (dist > 30f)
                interval = 1.0f;
            else if (dist > 15f)
                interval = 0.6f;
            else if (dist > 5f)
                interval = 0.25f;
            else
                interval = 0.08f;

            if (Time.time >= nextBeepTime)
            {
                audio.pitch = Mathf.Lerp(0.9f, 1.6f, Mathf.InverseLerp(50f, 0f, dist));
                audio.PlayOneShot(Plugin.Instance.SoundSonar);

                nextBeepTime = Time.time + interval;
            }
        }
    }
}