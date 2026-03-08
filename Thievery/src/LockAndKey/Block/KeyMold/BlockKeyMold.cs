using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Thievery.LockAndKey
{
    public class BlockKeyMold : BlockToolMold
    {
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            foreach (BlockBehavior blockBehavior in this.BlockBehaviors)
            {
                EnumHandling handled = EnumHandling.PreventDefault;
                blockBehavior.OnBlockBroken(world, pos, byPlayer, ref handled);
                if (handled == EnumHandling.PreventSubsequent)
                {
                    return;
                }
            }
            if (byPlayer?.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                world.BlockAccessor.SetBlock(0, pos);
                return;
            }
            var (savedKeyName, savedKeyUID) = LockManager.ExtractKeys(world.BlockAccessor.GetBlockEntity(pos), world.Api);

            if (string.IsNullOrEmpty(savedKeyName) || string.IsNullOrEmpty(savedKeyUID))
            {
                savedKeyName = "";
                savedKeyUID = "";
            }
            ItemStack itemstack = new ItemStack(world.GetBlock(new AssetLocation("thievery:keymold-burned-key-north")));

            if (itemstack.Attributes != null)
            {
                itemstack.Attributes.SetString("keyUID", savedKeyUID);
                itemstack.Attributes.SetString("keyName", savedKeyName);
            }
            world.SpawnItemEntity(itemstack, pos.ToVec3d().Add(0.5, 0.2, 0.5));
            world.BlockAccessor.SetBlock(0, pos);
            this.SpawnBlockBrokenParticles(pos);
        }
        public override string GetHeldItemName(ItemStack itemStack)
        {
            string customName = itemStack.Attributes.GetString("keyName", null);
            if (!string.IsNullOrEmpty(customName))
            {
                return $"{customName} Key Mold";
            }
            return Lang.Get("thievery:firedkeymold");
        }
    }
}
