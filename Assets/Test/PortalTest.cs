using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace Silly_Things.Codes.PortalTests
{
    public class PortalTest : NetworkBehaviour
    {
        [SerializeField]
        public PortalTest OtherPortal;

        public bool IsPlaced
        {
            get; private set;
        }

        private Collider wallCollider;

        public bool PlacePortal(Collider wall, Vector3 pos, Quaternion rot)
        {
            wallCollider = wall;

            transform.position = pos;
            transform.rotation = rot;

            gameObject.SetActive(true);
            IsPlaced = true;

            return true;
        }

        public void RemovePortal()
        {
            gameObject.SetActive(false);
            IsPlaced = false;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!IsServer)
                return;

            if (!IsPlaced || OtherPortal == null || !OtherPortal.IsPlaced)
                return;

            PlayerControllerB player = other.GetComponent<PlayerControllerB>();
            if (player == null)
                return;

            Vector3 localVel = transform.InverseTransformDirection(player.thisController.velocity);
            Vector3 newVel = OtherPortal.transform.TransformDirection(localVel);

            Vector3 exitPos = OtherPortal.transform.position + OtherPortal.transform.forward * 1.5f;

            player.TeleportPlayer(exitPos);
            player.externalForces = newVel;
        }
    }
}