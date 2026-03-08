using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Thievery.KeyHook
{
    public class BlockKeyHook : Block
    {
        public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityKeyHook beKeyHook = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityKeyHook;
            if (beKeyHook != null)
            {
                return beKeyHook.OnInteract(byPlayer, blockSel);
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}