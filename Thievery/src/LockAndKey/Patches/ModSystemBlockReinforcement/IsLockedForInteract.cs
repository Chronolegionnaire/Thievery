using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Thievery.LockAndKey.Patches.ModSystemBlockReinforcement
{
    [HarmonyPatch(typeof(Vintagestory.GameContent.ModSystemBlockReinforcement), "IsLockedForInteract")]
    public class Patch_ModSystemBlockReinforcement_IsLockedForInteract
    {
        static bool Prefix(BlockPos pos, IPlayer forPlayer, ref bool __result)
        {
            var world = forPlayer.Entity.World;
            var modSystem = world.Api.ModLoader.GetModSystem<ThieveryModSystem>();
            var lockManager = modSystem?.LockManager;

            if (lockManager == null)
            {
                __result = false;
                return false;
            }
            if (ThieveryModSystem.LoadedConfig.OwnerExempt && lockManager.IsPlayerAuthorized(pos, forPlayer))
            {
                return true;
            }
            var lockData = lockManager.GetLockData(pos);
            var heldItem = forPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible;
            if ((heldItem?.Code?.Path?.StartsWith("lockpick-") == true) || 
                (heldItem?.Code?.Path?.StartsWith("locktool-") == true) ||
                (heldItem?.Code?.Path?.StartsWith("key-") == true))
            {
                __result = false;
                return false;
            }
            if (lockData?.IsLocked == true)
            {
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }
    }
}