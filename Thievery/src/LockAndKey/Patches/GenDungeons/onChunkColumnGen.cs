using HarmonyLib;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace Thievery.Patches
{
    [HarmonyPatch(typeof(GenDungeons), "onChunkColumnGen")]
    public static class GenDungeons_onChunkColumnGen_Patch
    {
        private static readonly AccessTools.FieldRef<GenDungeons, ICoreServerAPI> sapiRef =
            AccessTools.FieldRefAccess<GenDungeons, ICoreServerAPI>("sapi");

        private static readonly AccessTools.FieldRef<GenDungeons, Dictionary<long, List<DungeonPlaceTask>>> tasksByRegionRef =
            AccessTools.FieldRefAccess<GenDungeons, Dictionary<long, List<DungeonPlaceTask>>>("dungeonPlaceTasksByRegion");

        [HarmonyPostfix]
        public static void Postfix(GenDungeons __instance, IChunkColumnGenerateRequest request)
        {
            var sapi = sapiRef(__instance);
            if (sapi == null || request?.Chunks == null || request.Chunks.Length == 0) return;

            var tasksByRegion = tasksByRegionRef(__instance);
            if (tasksByRegion == null) return;

            int regionSize = sapi.WorldManager.RegionSize;
            int regionx = request.ChunkX * 32 / regionSize;
            int regionz = request.ChunkZ * 32 / regionSize;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    long regionIndex = (long)(regionz + dz) * (sapi.WorldManager.MapSizeX / regionSize) + (regionx + dx);

                    if (!tasksByRegion.TryGetValue(regionIndex, out var tasks) || tasks == null) continue;

                    foreach (var placeTask in tasks)
                    {
                        if (placeTask?.TilePlaceTasks == null || placeTask.TilePlaceTasks.Count == 0) continue;
                        if (placeTask.SurfacePlaceTasks == null || placeTask.SurfacePlaceTasks.Count == 0) continue;

                        var start = placeTask.TilePlaceTasks[0].Pos;
                        string dungeonLockUid = $"dungeonlock_{placeTask.Code}_{start.X}_{start.Y}_{start.Z}";

                        WorldGenLockHelper.RegisterSurfaceTasks(placeTask.SurfacePlaceTasks, dungeonLockUid);
                    }
                }
            }
        }
    }
}