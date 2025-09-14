using HarmonyLib;
using Thievery.Config;
using Thievery.LockAndKey;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

[HarmonyPatch(typeof(BlockBehaviorReinforcable), nameof(BlockBehaviorReinforcable.OnBlockExploded))]
public static class Patch_OnBlockExploded_Prefix
{
    static bool Prefix(
        IWorldAccessor world,
        BlockPos pos,
        BlockPos explosionCenter,
        EnumBlastType blastType,
        ref EnumHandling handling)
    {
        var modSystemBlockReinforcement = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>(true);
        var bre = modSystemBlockReinforcement.GetReinforcment(pos);

        if (bre == null) return true;

        var cfg = ModConfig.Instance.Main;

        bool allow = cfg.ExplosionsAffectPlayerReinforcement ||
                     (bre.PlayerUID == "010100110100111101010011");

        int damage = 2;
        if (allow)
        {
            float rolled = cfg.ExplosionReinforcementDamage.NextFloat(world.Rand);
            damage = GameMath.Clamp(GameMath.RoundRandom(world.Rand, rolled), 0, int.MaxValue);
        }

        modSystemBlockReinforcement.ConsumeStrength(pos, damage);

        if (allow)
        {
            var be = world.BlockAccessor.GetBlockEntity(pos);
            if (be != null)
            {
                TryExplosionDamageContainerContents(world, pos);

                var lockData = be.GetBehavior<BlockEntityThieveryLockData>();
                if (lockData != null)
                {
                    bool hasLock =
                        !string.IsNullOrEmpty(lockData.LockUID) ||
                        !string.IsNullOrEmpty(lockData.LockType) ||
                        bre.Locked;

                    if (hasLock)
                    {
                        lockData.LockedState = false;
                        lockData.LockUID = null;
                        lockData.LockType = null;
                        be.MarkDirty(true);

                        if (world.Side == EnumAppSide.Server)
                        {
                            var sapi = world.Api as ICoreServerAPI;
                            sapi?.Network?.BroadcastBlockEntityPacket(pos, ThieveryPacketIds.LockStateSync, new LockData
                            {
                                LockUid = null,
                                IsLocked = false,
                                LockType = null
                            });
                        }
                    }
                }
            }
        }

        world.BlockAccessor.MarkBlockDirty(pos, (IPlayer)null);
        handling = EnumHandling.PreventDefault;
        return false;
    }

    private static void TryExplosionDamageContainerContents(IWorldAccessor world, BlockPos pos)
    {
        var chance = ModConfig.Instance?.Main?.ExplosionDamageContainerContentsChance ?? 0f;
        if (chance <= 0f) return;

        var be = world.BlockAccessor.GetBlockEntity(pos);
        if (be == null) return;

        bool changed = false;

        if (be is BlockEntityContainer bec && bec.Inventory != null)
        {
            for (int i = 0; i < bec.Inventory.Count; i++)
            {
                var slot = bec.Inventory[i];
                if (slot?.Empty == false && world.Rand.NextDouble() < chance)
                {
                    slot.TakeOutWhole();
                    slot.MarkDirty();
                    changed = true;
                }
            }
            if (changed) bec.MarkDirty(true);
            return;
        }

        if (be is BlockEntityGenericTypedContainer beg && beg.Inventory != null)
        {
            for (int i = 0; i < beg.Inventory.Count; i++)
            {
                var slot = beg.Inventory[i];
                if (slot?.Empty == false && world.Rand.NextDouble() < chance)
                {
                    slot.TakeOutWhole();
                    slot.MarkDirty();
                    changed = true;
                }
            }
            if (changed) beg.MarkDirty(true);
        }
    }
}
