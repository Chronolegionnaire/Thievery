using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Thievery.Config; // NEW

[HarmonyPatch(typeof(LandClaim), "TestPlayerAccess")]
public static class Patch_LandClaim_TestPlayerAccess
{
    static void Postfix(IPlayer player, EnumBlockAccessFlags claimFlag, ref EnumPlayerAccessResult __result, LandClaim __instance)
    {
        if (ModConfig.Instance?.Main?.BlockLockpickOnLandClaims == true)
        {
            return;
        }
        PropertyInfo blockSelProp = player.GetType().GetProperty("CurrentBlockSelection", BindingFlags.Instance | BindingFlags.Public);
        if (blockSelProp == null) return;

        var blockSelection = blockSelProp.GetValue(player) as BlockSelection;
        if (blockSelection == null) return;

        BlockPos pos = blockSelection.Position;
        var world = player.Entity.World;
        var block = world.BlockAccessor.GetBlock(pos);

        if (block?.CollectibleBehaviors != null &&
            block.CollectibleBehaviors.Any(b => b.GetType().Name == "BlockBehaviorLockable"))
        {
            __result = EnumPlayerAccessResult.OkOwner;
        }
    }
}