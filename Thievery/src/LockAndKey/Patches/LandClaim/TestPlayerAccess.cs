using System.Linq;
using HarmonyLib;
using Vintagestory.API.Common;

[HarmonyPatch(typeof(LandClaim), "TestPlayerAccess")]
public class Patch_LandClaim_TestPlayerAccess
{
    static bool Prefix(IPlayer player, EnumBlockAccessFlags claimFlag, ref EnumPlayerAccessResult __result, LandClaim __instance)
    {
        var world = player.Entity.World;
        var pos = player.Entity.Pos.AsBlockPos;
        var block = world.BlockAccessor.GetBlock(pos);
        if (block?.CollectibleBehaviors != null && 
            block.CollectibleBehaviors.Any(b => b.GetType().Name == "BlockBehaviorLockable"))
        {
            __result = EnumPlayerAccessResult.OkOwner;
            return false;
        }
        return true;
    }
}