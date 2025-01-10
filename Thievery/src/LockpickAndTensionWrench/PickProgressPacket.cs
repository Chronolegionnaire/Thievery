using ProtoBuf;

namespace Thievery.LockpickAndTensionWrench
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class PickProgressPacket
    {
        public float Progress { get; set; }
        public bool IsPicking { get; set; }
    }
}