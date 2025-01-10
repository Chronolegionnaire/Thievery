using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Thievery.LockAndKey
{
    public class LockManager
    {
        private ICoreAPI api;
        private ModSystemBlockReinforcement blockReinforcementSystem;

        public LockManager(ICoreAPI api)
        {
            this.api = api;
            this.blockReinforcementSystem = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
        }

        public void SetLock(BlockPos pos, string lockUid, bool isLocked = false, string lockType = null)
        {
            var blockEntity = api.World.BlockAccessor.GetBlockEntity(pos);
            if (blockEntity == null) return;

            var lockBehavior = blockEntity.GetBehavior<BlockEntityThieveryLockData>();
            if (lockBehavior == null) return;

            lockBehavior.LockUID = lockUid;
            lockBehavior.LockedState = isLocked;
            if (!string.IsNullOrEmpty(lockType))
            {
                lockBehavior.LockType = lockType;
            }

            blockEntity.MarkDirty(true);

            if (api.Side == EnumAppSide.Server)
            {
                var serverApi = api as ICoreServerAPI;
                serverApi?.Network?.BroadcastBlockEntityPacket(pos, ThieveryPacketIds.LockStateSync, new LockData
                {
                    LockUid = lockUid,
                    IsLocked = isLocked,
                    LockType = lockBehavior.LockType
                });
            }
        }

        public bool ToggleLock(BlockPos pos)
        {
            var blockEntity = api.World.BlockAccessor.GetBlockEntity(pos);
            if (blockEntity == null) return false;

            var lockBehavior = blockEntity.GetBehavior<BlockEntityThieveryLockData>();
            if (lockBehavior == null) return false;

            bool newState = !lockBehavior.LockedState;
            lockBehavior.LockedState = newState;
            blockEntity.MarkDirty(true);

            if (api.Side == EnumAppSide.Server)
            {
                var serverApi = api as ICoreServerAPI;
                serverApi?.Network?.BroadcastBlockEntityPacket(pos, ThieveryPacketIds.LockStateSync, new LockData
                {
                    LockUid = lockBehavior.LockUID,
                    IsLocked = newState,
                    LockType = lockBehavior.LockType
                });
            }
            return newState;
        }

        public LockData GetLockData(BlockPos pos)
        {
            var blockEntity = api.World.BlockAccessor.GetBlockEntity(pos);
            if (blockEntity == null) return null;

            var lockBehavior = blockEntity.GetBehavior<BlockEntityThieveryLockData>();
            if (lockBehavior == null) return null;

            return new LockData
            {
                LockUid = lockBehavior.LockUID,
                IsLocked = lockBehavior.LockedState,
                LockType = lockBehavior.LockType
            };
        }

        public bool IsPlayerAuthorized(BlockPos pos, IPlayer player)
        {
            var reinforcement = blockReinforcementSystem.GetReinforcment(pos);
            if (reinforcement == null) return false;
            if (reinforcement.PlayerUID == player.PlayerUID) return true;
            var playerGroup = player.GetGroup(reinforcement.GroupUid);
            if (playerGroup != null) return true;

            return false;
        }
        public static (string keyName, string keyUID) ExtractKeys(object obj, ICoreAPI api)
        {
            string keyName = null;
            string keyUID = null;

            if (obj == null)
            {
                return (null, null);
            }

            Type type = obj.GetType();
            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            {
                try
                {
                    var value = field.GetValue(obj);
                    if (field.Name == "keyName")
                    {
                        keyName = value as string;
                    }
                    else if (field.Name == "keyUID")
                    {
                        keyUID = value as string;
                    }
                }
                catch (Exception ex)
                {
                }
            }

            return (keyName, keyUID);
        }
    }
}
