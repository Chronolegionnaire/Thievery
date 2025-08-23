using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Thievery.LockAndKey
{
    [ProtoContract]
    public class LockData
    {
        [ProtoMember(1)]
        public string LockUid { get; set; }

        [ProtoMember(2)]
        public bool IsLocked { get; set; }

        [ProtoMember(3)]
        public string LockType { get; set; }

        [ProtoMember(4)]
        public long LockoutUntilMs { get; set; }
    }
    public static class ThieveryPacketIds
    {
        public const int LockStateSync = 2002;
    }
    [ProtoContract]
    public class TransformMoldPacket
    {
        [ProtoMember(1)]
        public BlockPos BlockPos { get; set; }

        [ProtoMember(2)]
        public int SlotIndex { get; set; }
        
        [ProtoMember(3)]
        public string KeyUID { get; set; }
        
        [ProtoMember(4)]
        public string KeyName { get; set; }
    }
    [ProtoContract]
    public class KeyNameUpdatePacket
    {
        [ProtoMember(1)]
        public int SlotId { get; set; }
        [ProtoMember(2)]
        public string KeyName { get; set; }
    }
    [ProtoContract]
    public class SyncKeyAttributesPacket
    {
        [ProtoMember(1)]
        public BlockPos BlockPos { get; set; }

        [ProtoMember(2)]
        public string KeyUID { get; set; }

        [ProtoMember(3)]
        public string KeyName { get; set; }
    }
    [ProtoContract]
    public class LockPickCompletePacket
    {
        [ProtoMember(1)]
        public BlockPos BlockPos { get; set; }
        
        [ProtoMember(2)]
        public string LockUid { get; set; }
        [ProtoMember(3)]
        public LockAction Action { get; set; }
    }
    [ProtoContract]
    public class StartLockpickSessionPacket {
        [ProtoMember(1)] public string ActiveInventoryId { get; set; }
        [ProtoMember(2)] public int ActiveSlotId { get; set; }
        [ProtoMember(3)] public string OffhandInventoryId { get; set; }
        [ProtoMember(4)] public int OffhandSlotId { get; set; }
        [ProtoMember(5)] public string ActiveToken { get; set; }
        [ProtoMember(6)] public string OffhandToken { get; set; }
    }

    [ProtoContract]
    public class EndLockpickSessionPacket {
        [ProtoMember(1)] public string ActiveInventoryId { get; set; }
        [ProtoMember(2)] public int ActiveSlotId { get; set; }
        [ProtoMember(3)] public string OffhandInventoryId { get; set; }
        [ProtoMember(4)] public int OffhandSlotId { get; set; }
    }
    [ProtoContract]
    public class LockPickLockoutPacket
    {
        [ProtoMember(1)]
        public BlockPos BlockPos { get; set; }

        [ProtoMember(2)]
        public string LockUid { get; set; }

        [ProtoMember(3)]
        public long LockoutUntilMs { get; set; }
    }
    [ProtoContract]
    public class WorldgenPickRewardPacket
    {
        [ProtoMember(1)]
        public BlockPos BlockPos;
        [ProtoMember(2)]
        public string LockUid;
        [ProtoMember(3)]
        public string LockType;
    }
    [ProtoContract]
    public class LockpickingXpAwardPacket
    {
        [ProtoMember(1)]
        public BlockPos BlockPos; // where the lock is
        [ProtoMember(2)]
        public string LockUid;    // identity
        [ProtoMember(3)]
        public string LockType;   // e.g. "padlock-steel"
    }
    public enum LockAction
    {
        Unlock = 0,
        Lock   = 1,
        Toggle = 2
    }
}