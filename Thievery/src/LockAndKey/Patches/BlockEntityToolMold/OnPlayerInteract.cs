using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Thievery.LockAndKey.Patches.BlockEntityToolMold;

[HarmonyPatch(typeof(Vintagestory.GameContent.BlockEntityToolMold), "OnPlayerInteract")]
public class Patch_BlockEntityToolMold_OnPlayerInteract
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result, Vintagestory.GameContent.BlockEntityToolMold __instance, IPlayer byPlayer, BlockFacing onFace, Vec3d hitPosition)
    {
        if (!(__instance.Block is BlockKeyMold))
            return true;

        var (savedKeyName, savedKeyUID) = LockManager.ExtractKeys(__instance, __instance.Api);
        if (__instance.Shattered || byPlayer.Entity.Controls.ShiftKey || byPlayer.Entity.Controls.HandUse != EnumHandInteract.None)
        {
            __result = false;
            return false;
        }

        var tryTakeContentsMethod = typeof(Vintagestory.GameContent.BlockEntityToolMold).GetMethod("TryTakeContents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        bool flag = (bool)tryTakeContentsMethod.Invoke(__instance, new object[] { byPlayer });

        if (!flag && __instance.FillLevel == 0)
        {
            ItemStack itemstack = new ItemStack(__instance.Api.World.GetBlock(new AssetLocation("thievery:keymold-burned-key-north")));

            if (string.IsNullOrEmpty(savedKeyName) || string.IsNullOrEmpty(savedKeyUID))
            {
                savedKeyName = "";
                savedKeyUID = "";
            }

            if (itemstack.Attributes != null)
            {
                itemstack.Attributes.SetString("keyUID", savedKeyUID);
                itemstack.Attributes.SetString("keyName", savedKeyName);
            }

            if (!byPlayer.InventoryManager.TryGiveItemstack(itemstack))
            {
                __instance.Api.World.SpawnItemEntity(itemstack, __instance.Pos.ToVec3d().Add(0.5, 0.2, 0.5));
            }

            __instance.Api.World.BlockAccessor.SetBlock(0, __instance.Pos);
            if (__instance.Block.Sounds?.Place != null)
            {
                __instance.Api.World.PlaySoundAt(__instance.Block.Sounds.Place, __instance.Pos, -0.5, byPlayer, false);
            }

            flag = true;
        }

        __result = flag;
        return false;
    }
}