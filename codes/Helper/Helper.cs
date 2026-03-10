using Silly_Things.codes.BountyContract;
using Unity.Netcode;
using UnityEngine;

namespace Silly_Things.codes.Helper
{
    internal class Helper
    {
        public static NetworkReference SpawnScrap(Item scrap, Vector3 position, int price)
        {
            Transform parent;
            if (RoundManager.Instance.spawnedScrapContainer == null)
                parent = StartOfRound.Instance.elevatorTransform;
            else
                parent = RoundManager.Instance.spawnedScrapContainer;

            GameObject gameObject = UnityEngine.Object.Instantiate(scrap.spawnPrefab, position + Vector3.up * 0.25f, Quaternion.identity, parent);
            GrabbableObject component = gameObject.GetComponent<GrabbableObject>();
            component.transform.rotation = Quaternion.Euler(component.itemProperties.restingRotation);
            component.scrapValue = price;
            component.fallTime = 1f;
            component.hasHitGround = true;
            component.reachedFloorTarget = true;
            //component.scrapValue = (int)(UnityEngine.Random.Range(scrap.minValue, scrap.maxValue) * RoundManager.Instance.scrapValueMultiplier);
            component.NetworkObject.Spawn(true);
            return new NetworkReference(gameObject.GetComponent<NetworkObject>(), component.scrapValue);
        }

        public static void LogDebugMod(string print, string value)
        {
            if (Plugin.SillyThingsConfig.debugMode.Value)
            {
                Plugin.Logger.LogWarning($"{print} : {value}");
            }
        }

        public static void LogDebugModError(string print, string value)
        {
            if (Plugin.SillyThingsConfig.debugMode.Value)
                Plugin.Logger.LogError($"{print} : {value}");
        }
    }
}
