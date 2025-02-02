using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Thievery.LockAndKey
{
    public class ItemKey : Item
    {
        public override string GetHeldItemName(ItemStack itemStack)
        {
            string customName = itemStack.Attributes.GetString("keyName", null);
            if (!string.IsNullOrEmpty(customName))
            {
                return customName;
            }
            return Lang.Get("thievery:key");
        }

        public override void OnHeldInteractStart(
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel,
            bool firstEvent,
            ref EnumHandHandling handling)
        {
            if (blockSel == null || !firstEvent) return;
            handling = EnumHandHandling.PreventDefault;
            var api = byEntity.World.Api;
            var player = (byEntity as EntityPlayer)?.Player;
            if (player == null) return;

            var blockPos = blockSel.Position;
            var block = api.World.BlockAccessor.GetBlock(blockPos);
            if (block is BlockGroundStorage && api.Side == EnumAppSide.Client)
            {
                HandleBlockKeyMoldPreInteraction(slot, byEntity, blockPos, block, ref handling);
                return;
            }
            HandleLockInteraction(slot, byEntity, blockSel, ref handling);
        }

        private void HandleLockInteraction(
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            ref EnumHandHandling handling)
        {
            var api = byEntity.World.Api;
            var player = (byEntity as EntityPlayer)?.Player;
            if (player == null || blockSel == null || !byEntity.Controls.Sneak) return;

            var thieveryModSystem = api.ModLoader.GetModSystem<ThieveryModSystem>();
            var lockManager = thieveryModSystem?.LockManager;
            if (lockManager == null) return;

            var keyUid = slot.Itemstack.Attributes.GetString("keyUID", "");
            BlockPos pos = blockSel.Position;
            var lockData = lockManager.GetLockData(pos);

            if (lockData == null)
            {
                return;
            }

            if (!lockManager.IsPlayerAuthorized(pos, player))
            {
                handling = EnumHandHandling.PreventDefault;
                return;
            }

            string blockLockUid = lockData.LockUid;

            if (string.IsNullOrEmpty(blockLockUid))
            {
                return;
            }

            if (string.IsNullOrEmpty(keyUid))
            {
                slot.Itemstack.Attributes.SetString("keyUID", blockLockUid);

                if (api.Side == EnumAppSide.Client)
                {
                    ICoreClientAPI capi = api as ICoreClientAPI;
                    ShowNameDialog(capi, slot);
                }
            }
            else
            {
                if (keyUid == blockLockUid)
                {
                    bool newLockState = lockManager.ToggleLock(pos);
                    PlayLockSound(api, pos, newLockState);
                }
            }

            handling = EnumHandHandling.PreventDefault;
        }

        private void HandleBlockKeyMoldPreInteraction(
            ItemSlot slot,
            EntityAgent byEntity,
            BlockPos blockPos,
            Block block,
            ref EnumHandHandling handling)
        {
            var api = byEntity.World.Api;
            var player = (byEntity as EntityPlayer)?.Player;
            if (player == null || !(block is BlockGroundStorage)) return;

            var groundStorageEntity = api.World.BlockAccessor.GetBlockEntity(blockPos) as BlockEntityGroundStorage;
            if (groundStorageEntity == null)
            {
                if (api.Side == EnumAppSide.Client)
                {
                    var capi = api as ICoreClientAPI;
                }
                return;
            }

            if (api.Side == EnumAppSide.Client)
            {
                var capi = api as ICoreClientAPI;
                for (int i = 0; i < groundStorageEntity.Inventory.Count; i++)
                {
                    var inventorySlot = groundStorageEntity.Inventory[i];

                    if (inventorySlot?.Itemstack?.Block?.Code?.Path.Contains("keymoldpre") == true)
                    {
                        string keyUID = slot.Itemstack.Attributes.GetString("keyUID", "");
                        string keyName = slot.Itemstack.Attributes.GetString("keyName", "");

                        capi.Network.GetChannel("thievery").SendPacket(new TransformMoldPacket
                        {
                            BlockPos = blockPos,
                            SlotIndex = i,
                            KeyUID = keyUID,
                            KeyName = keyName
                        });

                        handling = EnumHandHandling.PreventDefault;
                        return;
                    }
                }
            }


            if (api.Side == EnumAppSide.Server)
            {
            }
        }
        private void ShowNameDialog(ICoreClientAPI capi, ItemSlot slot)
        {
            KeyNamingDialog dialog = new KeyNamingDialog(slot, capi);
            dialog.OnNameSet += (newName) =>
            {
            };

            dialog.TryOpen();
            capi.Gui.RequestFocus(dialog);
        }
        private void PlayLockSound(ICoreAPI api, BlockPos pos, bool isLocked)
        {
            string soundPath = "thievery:sounds/lock";
            AssetLocation sound = new AssetLocation(soundPath);
            api.World.PlaySoundAt(
                sound,
                pos.X + 0.5,
                pos.Y + 0.5,
                pos.Z + 0.5,
                null,
                true,
                32f,
                1f
            );
        }
    }
}
