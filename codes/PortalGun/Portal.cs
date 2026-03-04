using GameNetcodeStuff;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Silly_Things.Codes.PortalGun
{
    public class Portal : NetworkBehaviour
    {
        private ulong linkedPortalId;
        private Renderer? portalRenderer;

        private HashSet<PlayerControllerB> teleportingPlayers = new HashSet<PlayerControllerB>();

        private bool isPortalA;
        public Dictionary<PlayerControllerB, float> teleportTimers = new Dictionary<PlayerControllerB, float>();

        public void Setup(bool portalType)
        {
            isPortalA = portalType;
            portalRenderer = GetComponentInChildren<Renderer>();
            if (portalRenderer != null)
            {
                portalRenderer.material.color = isPortalA ? new Color(0.2f, 0.4f, 1f) : new Color(1f, 0.5f, 0f);
            }
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

            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(linkedPortalId, out NetworkObject targetObj))
                return;

            Portal targetPortal = targetObj.GetComponent<Portal>();
            if (targetPortal == null)
                return;

            Vector3 localVel = transform.InverseTransformDirection(player.thisController.velocity);
            Vector3 newVel = targetPortal.transform.TransformDirection(localVel);

            Vector3 exitPos = targetPortal.transform.position + targetPortal.transform.forward * 1.5f;

            player.TeleportPlayer(exitPos);
            player.externalForces = newVel;

            targetPortal.AddTeleportingPlayer(player);
            AddTeleportingPlayer(player);
        }

        public void AddTeleportingPlayer(PlayerControllerB player)
        {
            teleportingPlayers.Add(player);
            teleportTimers[player] = Time.time + 0.2f;
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
