using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Thievery.KeyHook
{
    public class BlockKeyHook : Block
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }
        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityKeyHook beshelf = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityKeyHook;
            if (beshelf != null)
            {
                return beshelf.OnInteract(byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}