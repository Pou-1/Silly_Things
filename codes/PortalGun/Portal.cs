using GameNetcodeStuff;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Silly_Things.Codes.PortalGun
{
    public class Portal : NetworkBehaviour
    {
        private ulong linkedPortalId;

        private HashSet<PlayerControllerB> teleportingPlayers = new HashSet<PlayerControllerB>();

        private bool isPortalA;
        public Dictionary<PlayerControllerB, float> teleportTimers = new Dictionary<PlayerControllerB, float>();

        public void Setup(bool portalType)
        {
            isPortalA = portalType;
        }

        public void SetLinkedPortal(ulong id)
        {
            linkedPortalId = id;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!IsServer)
                return;

            PlayerControllerB player = other.GetComponent<PlayerControllerB>();
            if (player == null || teleportingPlayers.Contains(player))
                return;

            if (teleportTimers.TryGetValue(player, out float time))
            {
                if (Time.time < time)
                    return;
            }

            Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;

            if (Vector3.Dot(transform.forward, dirToPlayer) > 0f)
                return;

            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(linkedPortalId, out NetworkObject targetObj))
                return;

            Portal targetPortal = targetObj.GetComponent<Portal>();
            if (targetPortal == null)
                return;

            Vector3 localPos = transform.InverseTransformPoint(player.transform.position);

            Vector3 newWorldPos = targetPortal.transform.TransformPoint(localPos);

            newWorldPos += targetPortal.transform.forward * 1.0f;
            newWorldPos -= new Vector3(-1f, -1f, 0f);

            Vector3 localVel = transform.InverseTransformDirection(player.thisController.velocity);
            Vector3 newVel = targetPortal.transform.TransformDirection(localVel);

            Quaternion relativeRot = Quaternion.Inverse(transform.rotation) * player.transform.rotation;
            Quaternion newRot = targetPortal.transform.rotation * relativeRot;

            player.TeleportPlayer(newWorldPos);
            player.transform.rotation = newRot;
            player.externalForces = newVel;

            targetPortal.AddTeleportingPlayer(player);
            AddTeleportingPlayer(player);
        }

        public void AddTeleportingPlayer(PlayerControllerB player)
        {
            teleportingPlayers.Add(player);
            teleportTimers[player] = Time.time + 0.5f;
        }

        public void Update()
        {
            List<PlayerControllerB> toRemove = new List<PlayerControllerB>();
            foreach (var kvp in teleportTimers)
            {
                if (Time.time >= kvp.Value)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var p in toRemove)
            {
                teleportTimers.Remove(p);
                teleportingPlayers.Remove(p);
            }
        }
    }
}
