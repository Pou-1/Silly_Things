using Unity.Netcode;
using UnityEngine;

namespace Silly_Things.Codes.PortalGun
{
    public class PortalView : NetworkBehaviour
    {
        public Camera? portalCamera;
        public Renderer? portalScreenRenderer;
        public Portal? linkedPortal;
        public Transform? playerCameraTransform;

        public void LateUpdate()
        {
            if (linkedPortal == null || portalCamera == null || portalScreenRenderer == null)
                return;

            if (playerCameraTransform == null)
            {
                Plugin.Logger.LogError("PortalView: playerCameraTransform is null");
                return;
            }

            PortalView targetView = linkedPortal.GetComponent<PortalView>();
            if (targetView == null || targetView.portalCamera == null)
            {
                Plugin.Logger.LogError("PortalView: targetView or targetView.portalCamera is null");
                return;
            }

            if (portalCamera.targetTexture != null && portalScreenRenderer.material != null)
            {
                portalScreenRenderer.material.mainTexture = portalCamera.targetTexture;
            }

            Transform inPortal = transform;
            Transform outPortal = linkedPortal.transform;

            Vector3 relativePos = inPortal.InverseTransformPoint(playerCameraTransform.position);
            portalCamera.transform.position = outPortal.TransformPoint(relativePos);

            Quaternion relativeRot = Quaternion.Inverse(inPortal.rotation) * playerCameraTransform.rotation;
            portalCamera.transform.rotation = outPortal.rotation * relativeRot;

            if (portalCamera.targetTexture != null && portalScreenRenderer.material != null)
                portalScreenRenderer.material.mainTexture = portalCamera.targetTexture;
        }
    }
}
