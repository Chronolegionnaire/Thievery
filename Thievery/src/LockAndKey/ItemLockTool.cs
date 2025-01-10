using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Thievery.LockAndKey
{
    public class ItemLockTool : Item
    {
        private const string LOCKTOOL_ATTR = "lockUID";

        public override void OnHeldInteractStart(
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel,
            bool firstEvent,
            ref EnumHandHandling handling)
        {
            if (blockSel == null || !byEntity.Controls.Sneak || !firstEvent) return;

            var api = byEntity.World.Api;
            var player = (byEntity as EntityPlayer)?.Player;
            if (player == null) return;

            var thieveryModSystem = api.ModLoader.GetModSystem<ThieveryModSystem>();
            if (thieveryModSystem == null) return;
            var lockManager = thieveryModSystem.LockManager;
            if (lockManager == null) return;

            BlockPos pos = blockSel.Position;
            var lockData = lockManager.GetLockData(pos);
            if (lockData == null) return;

            if (!lockManager.IsPlayerAuthorized(pos, player))
            {
                handling = EnumHandHandling.PreventDefault;
                return;
            }

            string blockLockUid = lockData.LockUid;
            if (string.IsNullOrEmpty(blockLockUid)) return;

            string toolLockUid = slot.Itemstack.Attributes.GetString(LOCKTOOL_ATTR, "");

            if (string.IsNullOrEmpty(toolLockUid))
            {
                slot.Itemstack.Attributes.SetString(LOCKTOOL_ATTR, blockLockUid);
                PlayLockSound(api, pos);
                DamageItem(slot, 50, byEntity);
                handling = EnumHandHandling.PreventDefault;
            }
            else
            {
                lockData.LockUid = toolLockUid;
                lockManager.SetLock(pos, toolLockUid, lockData.IsLocked);
                PlayLockSound(api, pos);
                DamageItem(slot, 50, byEntity);
                slot.Itemstack.Attributes.SetString(LOCKTOOL_ATTR, "");
                handling = EnumHandHandling.PreventDefault;
            }
        }

        private bool DamageItem(ItemSlot itemSlot, int damage, EntityAgent byEntity)
        {
            var api = byEntity.World.Api;
            try
            {
                if (itemSlot?.Itemstack?.Collectible == null) return false;
                var originalItemstack = itemSlot.Itemstack;
                itemSlot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, itemSlot, damage);
                if (itemSlot.Itemstack == null || itemSlot.Itemstack != originalItemstack)
                {
                    itemSlot.MarkDirty();
                    return true;
                }
                itemSlot.MarkDirty();
            }
            catch (Exception)
            {
            }
            return false;
        }

        private void PlayLockSound(ICoreAPI api, BlockPos pos)
        {
            var soundLocation = new AssetLocation("thievery:sounds/lock");
            api.World.PlaySoundAt(
                soundLocation,
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
