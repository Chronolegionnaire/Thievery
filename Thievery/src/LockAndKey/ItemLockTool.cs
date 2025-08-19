using System;
using System.Collections.Generic;
using Thievery.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Thievery.LockAndKey
{
    public class ItemLockTool : Item
    {
        private const string LOCKTOOL_ATTR = "lockUID";
        private const string TOOLMODE_ATTR = "toolMode";
        private const string MODE_COPY = "copylock";
        private const string MODE_PASTE = "pastelock";

        private static SkillItem copyMode;
        private static SkillItem pasteMode;
        private readonly List<SkillItem> modes = new List<SkillItem>();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api.Side == EnumAppSide.Client)
            {
                var capi = api as ICoreClientAPI;

                copyMode = new SkillItem
                {
                    Code = new AssetLocation(MODE_COPY),
                    Name = Lang.Get("thievery:common-copy")
                }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(
                    new AssetLocation("game:textures/icons/copy.svg"),
                    48, 48, 5, ColorUtil.WhiteArgb));

                pasteMode = new SkillItem
                {
                    Code = new AssetLocation(MODE_PASTE),
                    Name = Lang.Get("thievery:common-paste")
                }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(
                    new AssetLocation("game:textures/icons/paste.svg"),
                    48, 48, 5, ColorUtil.WhiteArgb));
            }
            else
            {
                copyMode = new SkillItem { Code = new AssetLocation(MODE_COPY), Name = "Copy Lock UID" };
                pasteMode = new SkillItem { Code = new AssetLocation(MODE_PASTE), Name = "Paste Lock UID" };
            }

            modes.Clear();
            modes.Add(copyMode);
            modes.Add(pasteMode);
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            foreach (var mode in modes) mode?.Dispose();
            modes.Clear();
            base.OnUnloaded(api);
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return modes.ToArray();
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            var arr = GetToolModes(slot, byPlayer as IClientPlayer, blockSel);
            if (arr == null || arr.Length == 0)
            {
                slot.Itemstack.Attributes.SetString(TOOLMODE_ATTR, MODE_COPY);
            }
            else
            {
                int clamped = GameMath.Clamp(toolMode, 0, arr.Length - 1);
                string modeName = arr[clamped].Code.Path;
                slot.Itemstack.Attributes.SetString(TOOLMODE_ATTR, modeName);
            }
            slot.MarkDirty();
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            string current = slot.Itemstack.Attributes.GetString(TOOLMODE_ATTR, MODE_COPY);
            var arr = GetToolModes(slot, byPlayer as IClientPlayer, blockSel);
            if (arr == null || arr.Length == 0) return 0;

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].Code.Path == current) return i;
            }
            return 0;
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

            string currentMode = slot.Itemstack.Attributes.GetString(TOOLMODE_ATTR, MODE_COPY);
            if (currentMode == MODE_COPY)
            {
                string blockLockUid = lockData.LockUid;
                if (string.IsNullOrEmpty(blockLockUid)) return;

                slot.Itemstack.Attributes.SetString(LOCKTOOL_ATTR, blockLockUid);
                PlayLockSound(api, pos);
                DamageItem(slot, 50, byEntity);
                handling = EnumHandHandling.PreventDefault;
            }
            else
            {
                string toolLockUid = slot.Itemstack.Attributes.GetString(LOCKTOOL_ATTR, "");
                if (string.IsNullOrEmpty(toolLockUid)) return;

                lockData.LockUid = toolLockUid;
                lockManager.SetLock(pos, toolLockUid, lockData.IsLocked);

                PlayLockSound(api, pos);
                DamageItem(slot, ModConfig.Instance.Main.LockToolDamage, byEntity);
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
