using Unity.Netcode;
using UnityEngine;

namespace Silly_Things.Codes.PortalGun
{
    public class Portal : NetworkBehaviour
    {
        private ulong linkedPortalId;
        private bool isTeleporting;

        [SerializeField] private Renderer portalRenderer;

        public void SetIsPortalA(bool isA)
        {
            if (portalRenderer != null)
            {
                portalRenderer.material.color = isA
                    ? new Color(0.2f, 0.4f, 1f)
                    : new Color(1f, 0.5f, 0f);
            }
        }

        public void SetLinkedPortal(ulong id)
        {
            linkedPortalId = id;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer)
                return;

            if (isTeleporting)
                return;

            if (!other.CompareTag("Player"))
                return;

            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(linkedPortalId, out NetworkObject targetObj))
                return;

            Portal targetPortal = targetObj.GetComponent<Portal>();
            if (targetPortal == null)
                return;

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb == null)
                return;

            Transform inTransform = transform;
            Transform outTransform = targetPortal.transform;

            Vector3 localVel = inTransform.InverseTransformDirection(rb.velocity);
            Vector3 newVel = outTransform.TransformDirection(localVel);

            Vector3 exitPos = outTransform.position + outTransform.forward * 1.2f;

            rb.position = exitPos;
            rb.velocity = newVel;

            targetPortal.isTeleporting = true;
            Invoke(nameof(ResetTeleport), 0.2f);
        }

        private void ResetTeleport()
        {
            isTeleporting = false;
        }
    }
}
