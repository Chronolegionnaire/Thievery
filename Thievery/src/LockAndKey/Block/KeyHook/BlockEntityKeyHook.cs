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
		private InventoryGeneric inv;
		private Block block;
		private Matrixf mat = new Matrixf();
		public override InventoryBase Inventory
		{
			get
			{
				return this.inv;
			}
		}

		public override string InventoryClassName
		{
			get
			{
				return "keyhook";
			}
		}
		public override string AttributeTransformCode
		{
			get
			{
				return "onkeyhookTransform";
			}
		}
		public BlockEntityKeyHook()
		{
			this.inv = new InventoryGeneric(9, "keyhook-0", null, null);
		}
		public override void Initialize(ICoreAPI api)
		{
			this.block = api.World.BlockAccessor.GetBlock(this.Pos);
			base.Initialize(api);
			if (api is ICoreClientAPI)
			{
				this.mat.RotateYDeg(this.block.Shape.rotateY);
				api.Event.RegisterEventBusListener(new EventBusListenerDelegate(this.OnEventBusEvent), 0.5, null);
			}
		}

		private void OnEventBusEvent(string eventname, ref EnumHandling handling, IAttribute data)
		{
			if (eventname != "genjsontransform" && eventname != "oncloseedittransforms" && eventname != "onapplytransforms")
			{
				return;
			}
			if (this.Inventory.Empty)
			{
				return;
			}
			for (int i = 0; i < this.DisplayedItems; i++)
			{
				if (!this.Inventory[i].Empty)
				{
					string key = this.getMeshCacheKey(this.Inventory[i].Itemstack);
					base.MeshCache.Remove(key);
				}
			}
			this.updateMeshes();
			this.MarkDirty(true, null);
		}

		internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
		{
			ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
			ItemStack itemstack = slot.Itemstack;
			CollectibleObject colObj = (itemstack != null) ? itemstack.Collectible : null;
			bool hookable = ((colObj != null) ? colObj.Attributes : null) != null && colObj.Attributes["keyhookable"].AsBool(false);
			if (slot.Empty || !hookable)
			{
				return this.TryTake(byPlayer, blockSel);
			}
			if (!hookable)
			{
				return false;
			}
			ItemStack itemstack2 = slot.Itemstack;
			AssetLocation assetLocation;
			if (itemstack2 == null)
			{
				assetLocation = null;
			}
			else
			{
				Block block = itemstack2.Block;
				if (block == null)
				{
					assetLocation = null;
				}
				else
				{
					BlockSounds sounds = block.Sounds;
					assetLocation = ((sounds != null) ? sounds.Place : null);
				}
			}
			AssetLocation sound = assetLocation;
			ItemStack itemstack3 = slot.Itemstack;
			AssetLocation stackName = (itemstack3 != null) ? itemstack3.Collectible.Code : null;
			if (this.TryPut(slot, blockSel))
			{
				this.Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("game:sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
				this.Api.World.Logger.Audit("{0} Put 1x{1} into Hook at {2}.", new object[]
				{
					byPlayer.PlayerName,
					stackName,
					this.Pos
				});
				return true;
			}
			return false;
		}

		private bool TryPut(ItemSlot slot, BlockSelection blockSel)
		{
			int index = blockSel.SelectionBoxIndex;
			for (int i = 0; i < this.inv.Count; i++)
			{
				int slotnum = (index + i) % this.inv.Count;
				if (this.inv[slotnum].Empty)
				{
					int num = slot.TryPutInto(this.Api.World, this.inv[slotnum], 1);
					this.MarkDirty(false, null);
					return num > 0;
				}
			}
			return false;
		}

		private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
		{
			int index = blockSel.SelectionBoxIndex;
			if (!this.inv[index].Empty)
			{
				ItemStack stack = this.inv[index].TakeOut(1);
				if (byPlayer.InventoryManager.TryGiveItemstack(stack, false))
				{
					Block block = stack.Block;
					AssetLocation assetLocation;
					if (block == null)
					{
						assetLocation = null;
					}
					else
					{
						BlockSounds sounds = block.Sounds;
						assetLocation = ((sounds != null) ? sounds.Place : null);
					}
					AssetLocation sound = assetLocation;
					this.Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("game:sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
				}
				if (stack.StackSize > 0)
				{
					this.Api.World.SpawnItemEntity(stack, this.Pos, null);
				}
				this.Api.World.Logger.Audit("{0} Took 1x{1} from Hook at {2}.", new object[]
				{
					byPlayer.PlayerName,
					stack.Collectible.Code,
					this.Pos
				});
				this.MarkDirty(false, null);
				return true;
			}
			return false;
		}
		protected override float[][] genTransformationMatrices()
		{
			int topCount = 5;
			int bottomCount = 4;

			float stepX = 0.204f;
			
			float stepXBottom = 0.203f;

			float yTop =  +0.12f;
			float yBottom = -0.045f;

			float startXTop = -0.4f;
			float startXBottom = -0.39f + stepX/2;

			float zFront = 0f;
			float[][] tfMatrices = new float[this.Inventory.Count][];
			for (int index = 0; index < tfMatrices.Length; index++)
			{
				float x, y;

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
					.RotateYDeg(this.block.Shape.rotateY)
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
			ItemSlot slot = this.inv[index];
			if (slot.Empty)
			{
				sb.AppendLine(Lang.Get("Empty", Array.Empty<object>()));
				return;
			}
			sb.AppendLine(slot.Itemstack.GetName());
		}
		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
		{
			base.FromTreeAttributes(tree, worldForResolving);
			this.RedrawAfterReceivingTreeAttributes(worldForResolving);
		}
	}
}
