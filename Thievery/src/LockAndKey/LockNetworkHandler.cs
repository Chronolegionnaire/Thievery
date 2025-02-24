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
    }
}