using System;
using Thievery.Config;
using Thievery.LockpickAndTensionWrench;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Thievery.LockAndKey
{
    public class ItemKey : Item
    {
        public override string GetHeldItemName(ItemStack stack)
        {
            string customName = stack.Attributes.GetString("keyName", null);
            if (!string.IsNullOrEmpty(customName)) return customName;
            return base.GetHeldItemName(stack);
        }

        public override void OnHeldInteractStart(
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel,
            bool firstEvent,
            ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            if (blockSel == null || !firstEvent)
            {
                return;
            }

            handling = EnumHandHandling.PreventDefault;

            var api = byEntity.World.Api;
           
            var player = (byEntity as EntityPlayer)?.Player;
            if (player == null)
            {
                return;
            }
            var blockPos = blockSel.Position;
            var block = api.World.BlockAccessor.GetBlock(blockPos);
            var groundStorageEntity = api.World.BlockAccessor.GetBlockEntity(blockPos) as BlockEntityGroundStorage;
            if (groundStorageEntity != null)
            {
                int i = 0;

                var inventorySlot = groundStorageEntity.Inventory[i];
                if (string.Equals(inventorySlot?.Itemstack?.Block?.Code?.Path, "keymoldpre", StringComparison.OrdinalIgnoreCase))
                {
                    HandleBlockKeyMoldPreInteraction(slot, byEntity, blockPos, block, ref handling);
                    return;
                }
            }
            HandleLockInteraction(slot, byEntity, blockSel, ref handling);
        }


        private void HandleLockInteraction(
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            ref EnumHandHandling handling)
        {
            if (byEntity?.World?.Api == null) return;
            if (slot?.Itemstack?.Attributes == null) return;
            if (blockSel == null) return;

            var api = byEntity.World.Api;
            var player = (byEntity as EntityPlayer)?.Player;
            if (player == null) return;

            handling = EnumHandHandling.PreventDefault;

            var thieveryModSystem = api.ModLoader.GetModSystem<ThieveryModSystem>();
            var lockManager = thieveryModSystem?.LockManager;
            if (lockManager == null) return;

            BlockPos pos = blockSel.Position;
            var lockData = lockManager.GetLockData(pos);
            if (lockData == null || string.IsNullOrEmpty(lockData.LockUid))
            {
                return;
            }

            string keyUid = slot.Itemstack.Attributes.GetString("keyUID", "");
            string blockLockUid = lockData.LockUid;
            if (string.IsNullOrEmpty(keyUid))
            {
                if (api.Side == EnumAppSide.Client)
                {
                    var capi = api as ICoreClientAPI;
                    capi?.Network.GetChannel("thievery").SendPacket(new BindKeyToLockPacket
                    {
                        BlockPos = pos
                    });
                }

                return;
            }
            if (slot.Itemstack.Collectible.Code.Path == "key-aged")
            {
                Random rnd = new Random();
                if (rnd.NextDouble() < ModConfig.Instance.Main.AgedKeyDamageChance)
                {
                    DamageItem(slot, ModConfig.Instance.Main.AgedKeyDamage, byEntity);
                }
            }

            if (keyUid == blockLockUid)
            {
                if (api.Side == EnumAppSide.Client)
                {
                    var capi = api as ICoreClientAPI;
                    capi?.Network.GetChannel("thievery").SendPacket(new UseKeyOnLockPacket
                    {
                        BlockPos = pos,
                        KeyUid = keyUid
                    });

                    if (slot.Itemstack.Collectible.Code.Path == "key-aged")
                    {
                        capi?.Network.GetChannel("thievery").SendPacket(new WorldgenPickRewardPacket
                        {
                            BlockPos = pos,
                            LockUid = lockData.LockUid,
                            LockType = lockData.LockType
                        });
                    }
                }
                return;
            }
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

        private bool DamageItem(ItemSlot itemSlot, int damage, EntityAgent byEntity)
        {
            try
            {
                if (api.Side != EnumAppSide.Client)
                {
                    return false;
                }

                if (itemSlot == null || itemSlot.Itemstack == null || itemSlot.Itemstack.Collectible == null)
                {
                    return false;
                }

                int durability = itemSlot.Itemstack.Attributes.GetInt("durability", 0);
                bool willBreak = durability <= damage;
                var clientApi = api as ICoreClientAPI;
                clientApi.Network.GetChannel("thievery").SendPacket(new ItemDamagePacket
                {
                    InventoryId = itemSlot.Inventory.InventoryID,
                    SlotId = itemSlot.Inventory.GetSlotId(itemSlot),
                    Damage = damage
                });
                return willBreak;
            }
            catch (Exception ex)
            {
                api.World.Logger.Error("DamageItem: Exception during DamageItem. {0}", ex);
                return false;
            }
        }
    }
}
