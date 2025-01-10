using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Thievery.LockAndKey.Patches.PlumbAndSquare;

[HarmonyPatch(typeof(ItemPlumbAndSquare), nameof(ItemPlumbAndSquare.OnHeldAttackStart))]
public static class Patch_ItemPlumbAndSquare_OnHeldAttackStart
{
    static void Prefix(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling, out bool __state)
    {
        __state = false;

        if (blockSel == null || byEntity.World.Side == EnumAppSide.Client)
        {
            return;
        }

        ICoreAPI api = byEntity.Api;
        IServerPlayer player = (byEntity as EntityPlayer)?.Player as IServerPlayer;
        if (player == null) return;

        LockManager lockManager = new LockManager(api);
        if (!lockManager.IsPlayerAuthorized(blockSel.Position, player))
        {
            player.SendIngameError("notauthorized", "You are not authorized to remove this lock.");
            return;
        }
        LockData lockData = lockManager.GetLockData(blockSel.Position);
        if (lockData != null && lockData.LockUid != null)
        {
            lockManager.SetLock(blockSel.Position, null, false);
            __state = true;
            handling = EnumHandHandling.PreventDefaultAction;
        }
    }

    static void Postfix(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling, bool __state)
    {
        if (__state)
        {
            handling = EnumHandHandling.PreventDefaultAction;
            return;
        }
    }
}