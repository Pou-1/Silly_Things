using Unity.Netcode;

namespace Silly_Things.codes.BountyContract
{
    public class NetworkReference
    {
        public NetworkObjectReference netObjectRef;
        public int value;

        public NetworkReference(NetworkObjectReference netObjectRef, int value)
        {
            this.netObjectRef = netObjectRef;
            this.value = value;
        }
    }
}
