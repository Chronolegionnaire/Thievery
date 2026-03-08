using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Thievery.KeyHook
{
    public class BlockEntityKeyHook : BlockEntityDisplay
    {
        private readonly InventoryGeneric inv;
        private Block block;

        public override InventoryBase Inventory => inv;
        public override string InventoryClassName => "keyhook";
        public override string AttributeTransformCode => "onkeyhookTransform";

        public BlockEntityKeyHook()
        {
            inv = new InventoryGeneric(9, "keyhook-0", null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            block = api.World.BlockAccessor.GetBlock(Pos);
            base.Initialize(api);

            // No need to register our own transform-edit event listener anymore.
            // BlockEntityDisplay already does that in 1.22.
        }

        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            ItemStack itemstack = slot.Itemstack;
            CollectibleObject colObj = itemstack?.Collectible;

            bool hookable = colObj?.Attributes?["keyhookable"].AsBool(false) == true;

            if (slot.Empty || !hookable)
            {
                return TryTake(byPlayer, blockSel);
            }

            AssetLocation sound = GetPlaceSound(slot.Itemstack);
            AssetLocation stackName = slot.Itemstack?.Collectible?.Code;

            if (TryPut(slot, blockSel))
            {
                Api.World.PlaySoundAt(
                    sound ?? new AssetLocation("game:sounds/player/build"),
                    byPlayer.Entity,
                    byPlayer,
                    true,
                    16f,
                    1f
                );

                Api.World.Logger.Audit(
                    "{0} Put 1x{1} into Hook at {2}.",
                    byPlayer.PlayerName,
                    stackName,
                    Pos
                );

                return true;
            }

            return false;
        }

        private bool TryPut(ItemSlot slot, BlockSelection blockSel)
        {
            int index = blockSel.SelectionBoxIndex;

            for (int i = 0; i < inv.Count; i++)
            {
                int slotnum = (index + i) % inv.Count;
                if (inv[slotnum].Empty)
                {
                    int moved = slot.TryPutInto(Api.World, inv[slotnum], 1);
                    MarkDirty(false);
                    return moved > 0;
                }
            }

            return false;
        }

        private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
        {
            int index = blockSel.SelectionBoxIndex;

            if (index < 0 || index >= inv.Count || inv[index].Empty)
            {
                return false;
            }

            ItemStack stack = inv[index].TakeOut(1);

            if (byPlayer.InventoryManager.TryGiveItemstack(stack, false))
            {
                AssetLocation sound = GetPlaceSound(stack);

                Api.World.PlaySoundAt(
                    sound ?? new AssetLocation("game:sounds/player/build"),
                    byPlayer.Entity,
                    byPlayer,
                    true,
                    16f,
                    1f
                );
            }

            if (stack.StackSize > 0)
            {
                Api.World.SpawnItemEntity(stack, Pos);
            }

            Api.World.Logger.Audit(
                "{0} Took 1x{1} from Hook at {2}.",
                byPlayer.PlayerName,
                stack.Collectible.Code,
                Pos
            );

            MarkDirty(false);
            return true;
        }

        private static AssetLocation GetPlaceSound(ItemStack stack)
        {
            Block block = stack?.Block;
            if (block?.Sounds == null) return null;

            return block.Sounds.Place.Location;
        }

        protected override float[][] genTransformationMatrices()
        {
            int topCount = 5;
            int bottomCount = 4;

            float stepX = 0.204f;
            float stepXBottom = 0.203f;

            float yTop = 0.12f;
            float yBottom = -0.045f;

            float startXTop = -0.4f;
            float startXBottom = -0.39f + stepX / 2f;

            float zFront = 0f;

            float[][] tfMatrices = new float[Inventory.Count][];

            for (int index = 0; index < tfMatrices.Length; index++)
            {
                float x;
                float y;

                if (index < topCount)
                {
                    int col = index;
                    x = startXTop + stepX * col;
                    y = yTop;
                }
                else
                {
                    int col = index - topCount;
                    if (col >= bottomCount) col = bottomCount - 1;

                    x = startXBottom + stepXBottom * col;
                    y = yBottom;
                }

                tfMatrices[index] = new Matrixf()
                    .Translate(0.5f, 0f, 0.5f)
                    .RotateYDeg(block.Shape.rotateY)
                    .Translate(x, y, zFront)
                    .Translate(-1.325f, 0.6f, -1.375f)
                    .Values;
            }

            return tfMatrices;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            if (forPlayer.CurrentBlockSelection == null)
            {
                base.GetBlockInfo(forPlayer, sb);
                return;
            }

            int index = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
            if (index < 0 || index >= inv.Count)
            {
                base.GetBlockInfo(forPlayer, sb);
                return;
            }

            ItemSlot slot = inv[index];

            if (slot.Empty)
            {
                sb.AppendLine(Lang.Get("Empty"));
                return;
            }

            sb.AppendLine(slot.Itemstack.GetName());
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            RedrawAfterReceivingTreeAttributes(worldForResolving);
        }
    }
}