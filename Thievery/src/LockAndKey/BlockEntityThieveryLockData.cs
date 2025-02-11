using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Thievery.LockAndKey
{
    public class BlockEntityThieveryLockData : BlockEntityBehavior
    {
        private string lockUID;
        private bool lockedState;
        private string lockType;

        public string LockUID
        {
            get => lockUID;
            set
            {
                lockUID = value;
                Blockentity.MarkDirty();
            }
        }
        
        public bool LockedState
        {
            get => lockedState;
            set
            {
                lockedState = value;
                Blockentity.MarkDirty();
            }
        }
        public string LockType
        {
            get => lockType;
            set
            {
                lockType = value;
                Blockentity.MarkDirty();
            }
        }

        public BlockEntityThieveryLockData(BlockEntity blockEntity) : base(blockEntity)
        {
            lockedState = false;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("lockUID", lockUID);
            tree.SetBool("lockedState", lockedState);
            tree.SetString("lockType", lockType);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);
            lockUID = tree.GetString("lockUID", null);
            lockedState = tree.GetBool("lockedState", false);
            lockType = tree.GetString("lockType", null);
        }

        public override void GetBlockInfo(IPlayer forPlayer, System.Text.StringBuilder description)
        {
            base.GetBlockInfo(forPlayer, description);

            if (!string.IsNullOrWhiteSpace(lockUID) && !description.ToString().Contains("Locked State:"))
            {
                description.AppendLine($"Locked State: {(lockedState ? "Locked" : "Unlocked")}");
            }
        }
    }
}
