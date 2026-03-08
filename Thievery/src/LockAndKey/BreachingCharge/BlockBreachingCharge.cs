using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Thievery.LockAndKey.BreachingCharge
{
    public class BlockBreachingCharge : Block, IIgnitable
    {
        private WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api.Side != EnumAppSide.Client) return;

            interactions = ObjectCacheUtil.GetOrCreate(api, "breachingChargeInteractions", () =>
            {
                var canIgniteStacks = BlockBehaviorCanIgnite.CanIgniteStacks(api, false);
                return new[]
                {
                    new WorldInteraction
                    {
                        MouseButton = EnumMouseButton.Right,
                        ActionLangCode = "blockhelp-bomb-ignite",
                        Itemstacks = canIgniteStacks.ToArray(),
                        GetMatchingStacks = (wi, bs, es) =>
                        {
                            var be = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityBreachingCharge;
                            return (be != null && !be.IsLit) ? wi.Itemstacks : null;
                        }
                    }
                };
            });
        }

        EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
            => EnumIgniteState.NotIgnitable;

        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            var be = byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityBreachingCharge;
            if (be == null || be.IsLit) return EnumIgniteState.NotIgnitablePreventDefault;
            return secondsIgniting > 0.75f ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }

        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            if (secondsIgniting < 0.7f) return;
            handling = EnumHandling.PreventDefault;

            IPlayer byPlayer = (byEntity as EntityPlayer != null)
                ? byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID)
                : null;

            var be = byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityBreachingCharge;
            be?.OnIgnite(byPlayer);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityBreachingCharge;
            if (be != null && be.CascadeLit) return Array.Empty<ItemStack>();
            return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection sel, IPlayer forPlayer)
            => interactions.Append(base.GetPlacedBlockInteractionHelp(world, sel, forPlayer));
    }
}
