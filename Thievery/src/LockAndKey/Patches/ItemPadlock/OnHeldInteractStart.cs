using System;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Thievery.LockAndKey.Patches.ItemPadlock
{
    [HarmonyPatch(typeof(Vintagestory.GameContent.ItemPadlock), "OnHeldInteractStart")]
    public class Patch_ItemPadlock_OnHeldInteractStart
    {
        static bool Prefix(
            Vintagestory.GameContent.ItemPadlock __instance,
            EntityAgent byEntity,
            BlockSelection blockSel,
            ref EnumHandHandling handling)
        {
            if (blockSel == null) return false;
            var api = byEntity.World.Api;
            var pos = blockSel.Position;
            var block = byEntity.World.BlockAccessor.GetBlock(pos);
            if (block is BlockDoor door && door.IsUpperHalf())
            {
                return false;
            }
            if (block.Code.PathStartsWith("multiblock"))
            {
                return false;
            }
            var modSystem = api.ModLoader.GetModSystem<ThieveryModSystem>();
            var lockManager = modSystem?.LockManager;
            handling = EnumHandHandling.PreventDefault;
            var modBre = api.ModLoader.GetModSystem<Vintagestory.GameContent.ModSystemBlockReinforcement>();
            var player = (byEntity as EntityPlayer)?.Player;
            if (modBre.IsReinforced(pos))
            {
                var lockData = lockManager.GetLockData(pos);
                if (lockData?.LockUid != null)
                {
                    return true;
                }


                if (api.Side == EnumAppSide.Server)
                {
                    string padlockCode = __instance?.Code?.Path ?? "padlock-unknown";
                    string lockUid = Guid.NewGuid().ToString();
                    lockManager.SetLock(
                        pos,
                        lockUid,
                        false,
                        padlockCode
                    );
                    string soundPath = "thievery:sounds/lock";
                    AssetLocation sound = new AssetLocation(soundPath);
                    api.World.PlaySoundAt(
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

                return true;
            }

            return true;
        }
    }
}
