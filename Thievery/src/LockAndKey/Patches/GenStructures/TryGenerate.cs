using HarmonyLib;
using System;
using System.Linq;
using Thievery.Config;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.ServerMods;

namespace Thievery.Patches
{
    [HarmonyPatch(typeof(WorldGenStructure), "TryGenerate")]
    public static class WorldGenStructure_TryGenerate_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(
            WorldGenStructure __instance,
            bool __result,
            IBlockAccessor blockAccessor,
            IWorldAccessor worldForCollectibleResolve)
        {
            if (!__result || __instance.LastPlacedSchematicLocation == null) return;

            var api = worldForCollectibleResolve?.Api;
            if (api == null) return;

            string gen = __instance.Code?.ToString() ?? "";
            string schem = (__instance.LastPlacedSchematic?.FromFile?.ToShortString() ?? "").Replace("\\", "/");
            string key = $"{gen}:{schem}";
            bool isBlacklisted = ModConfig.Instance.Blacklist.StructureBlacklist.Any(entry =>
            {
                if (string.IsNullOrWhiteSpace(entry)) return false;

                string normalized = entry.Replace("\\", "/");
                return key.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                       schem.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                       schem.EndsWith(normalized, StringComparison.OrdinalIgnoreCase);
            });
            if (isBlacklisted)
            {
                return;
            }

            if (!WorldGenLockHelper.CanProcess(api, blockAccessor, out var worldGen)) return;

            Cuboidi location = __instance.LastPlacedSchematicLocation;
            Random rand = WorldGenLockHelper.CreateDeterministicRandom(api.World.Seed, location.MinX, location.MinY, location.MinZ);
            string structureLockUid = $"structlock_{location.MinX}_{location.MinY}_{location.MinZ}";

            WorldGenLockHelper.ProcessArea(api, blockAccessor, worldGen, location, structureLockUid, rand);
        }
    }
}