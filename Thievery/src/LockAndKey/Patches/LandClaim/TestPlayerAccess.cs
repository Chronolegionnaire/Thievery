using System.Linq;
using System.Reflection;
using HarmonyLib;
using Thievery.Config;
using Vintagestory.API.Common;

[HarmonyPatch(typeof(LandClaim), "TestPlayerAccess")]
public static class Patch_LandClaim_TestPlayerAccess
{
    static void Postfix(IPlayer player, EnumBlockAccessFlags claimFlag, ref EnumPlayerAccessResult __result, LandClaim __instance)
    {
        if (ModConfig.Instance?.Main?.BlockLockpickOnLandClaims == true) return;
        if ((claimFlag & EnumBlockAccessFlags.Use) == 0) return;

        var blockSelProp = player.GetType().GetProperty("CurrentBlockSelection", BindingFlags.Instance | BindingFlags.Public);
        var blockSelection = blockSelProp?.GetValue(player) as BlockSelection;
        if (blockSelection == null) return;

        var world = player.Entity.World;
        var block = world.BlockAccessor.GetBlock(blockSelection.Position);
        if (block?.CollectibleBehaviors == null) return;

        if (block.CollectibleBehaviors.Any(b => b.GetType().Name == "BlockBehaviorLockable"))
        {
            if (__result == EnumPlayerAccessResult.Denied)
            {
                __result = EnumPlayerAccessResult.OkOwner;
            }
        }
    }
}