using System;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Thievery.LockAndKey.Patches.BlockBehaviorLockable
{
    [HarmonyPatch(typeof(Vintagestory.GameContent.BlockBehaviorLockable))]
    [HarmonyPatch("OnBlockInteractStart")]
    public static class Patch_BlockBehaviorLockable_OnBlockInteractStart
    {
        static bool Prefix(
            IWorldAccessor world,
            IPlayer byPlayer,
            BlockSelection blockSel,
            ref EnumHandling handling,
            ref bool __result)
        {
            var modSystem = world.Api.ModLoader.GetModSystem<ThieveryModSystem>();
            var lockManager = modSystem?.LockManager;
            if (lockManager == null) return true;
            var pos = blockSel?.Position;
            if (pos == null) return true;
            var lockData = lockManager.GetLockData(pos);
            var heldItem = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible;
            if ((heldItem?.Code?.Path?.StartsWith("lockpick-") == true) || 
                (heldItem?.Code?.Path?.StartsWith("locktool-") == true) ||
                (heldItem?.Code?.Path?.StartsWith("key-") == true))
            {
                handling = EnumHandling.PreventSubsequent;
                __result = true;
                return false;
            }

            if (lockData?.IsLocked == true)
            {
                if (ThieveryModSystem.LoadedConfig.OwnerExempt && lockManager.IsPlayerAuthorized(pos, byPlayer))
                {
                    return true;
                }

                if (world.Side == EnumAppSide.Client)
                {
                    var capi = world.Api as ICoreClientAPI;
                    capi?.TriggerIngameError("thieverymod-lockable", "locked", "This block is locked!");
                    string soundPath = "game:sounds/tool/padlock";
                    AssetLocation sound = new AssetLocation(soundPath);
                    capi.World.PlaySoundAt(
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
                handling = EnumHandling.PreventSubsequent;
                __result = false;
                return false;
            }
            return true;
        }
    }
}