using ProtoBuf;

namespace Thievery.LockpickAndTensionWrench
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class PickProgressPacket
    {
        public float Progress { get; set; }
        public bool IsPicking { get; set; }
    }
    [ProtoContract]
    public class ItemDamagePacket
    {
        [ProtoMember(1)]
        public string InventoryId { get; set; }
        [ProtoMember(2)]
        public int SlotId { get; set; }
        [ProtoMember(3)]
        public int Damage { get; set; }
    }
    [ProtoContract]
    public class ItemDestroyedPacket
    {
        [ProtoMember(1)]
        public string PlayerUid { get; set; }
    }
}