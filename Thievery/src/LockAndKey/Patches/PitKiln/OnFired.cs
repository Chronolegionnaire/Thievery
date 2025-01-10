using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Thievery.LockAndKey.Patches.PitKiln
{
    [HarmonyPatch(typeof(BlockEntityPitKiln))]
    [HarmonyPatch("OnFired")]
    public static class BlockEntityPitKilnPatch
    {
        // Prefix to capture state before the method runs
        static void Prefix(BlockEntityPitKiln __instance, out Dictionary<int, TreeAttribute> __state)
        {
            // Save the key attributes from each slot in a dictionary
            __state = new Dictionary<int, TreeAttribute>();
            for (int i = 0; i < __instance.Inventory.Count; i++)
            {
                var slot = __instance.Inventory[i];
                if (!slot.Empty && (slot.Itemstack?.Block?.Code?.Path.Contains("keymold-raw")) == true)
                {
                    var attributes = slot.Itemstack.Attributes.Clone() as TreeAttribute;
                    if (attributes != null)
                    {
                        __state[i] = attributes; // Save the slot index and its attributes
                    }
                }
            }
        }

        static void Postfix(BlockEntityPitKiln __instance, Dictionary<int, TreeAttribute> __state)
        {
            foreach (var kvp in __state)
            {
                int slotIndex = kvp.Key;
                TreeAttribute savedAttributes = kvp.Value;

                var slot = __instance.Inventory[slotIndex];
                if (!slot.Empty && slot.Itemstack?.Block?.Code?.Path.Contains("keymold-burned") == true)
                {
                    if (savedAttributes.HasAttribute("keyUID"))
                    {
                        slot.Itemstack.Attributes.SetString("keyUID", savedAttributes.GetString("keyUID"));
                    }
                    if (savedAttributes.HasAttribute("keyName"))
                    {
                        slot.Itemstack.Attributes.SetString("keyName", savedAttributes.GetString("keyName"));
                    }
                    slot.MarkDirty();
                    var api = __instance.Api;
                    if (api.Side == EnumAppSide.Client)
                    {
                        var clientApi = api as ICoreClientAPI;
                        clientApi.Network.GetChannel("thievery").SendPacket(new TransformMoldPacket
                        {
                            SlotIndex = slotIndex,
                            KeyUID = savedAttributes.GetString("keyUID"),
                            KeyName = savedAttributes.GetString("keyName"),
                            BlockPos = __instance.Pos
                        });
                    }
                }
            }
        }
    }
}
