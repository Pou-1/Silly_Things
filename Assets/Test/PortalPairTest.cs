using Silly_Things.Codes.PortalTests;
using UnityEngine;

namespace Silly_Things.codes.PortalTests
{
    public class PortalPairTest : MonoBehaviour
    {
        public PortalTest[] Portals
        {
            get; private set;
        }

        public void Awake()
        {
            Portals = GetComponentsInChildren<PortalTest>();

            if (Portals.Length != 2)
            {
                Debug.LogError("PortalPair must contain exactly 2 PortalTest components.");
                return;
            }

            Portals[0].OtherPortal = Portals[1];
            Portals[1].OtherPortal = Portals[0];
        }
    }
}
