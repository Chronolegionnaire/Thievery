using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace Thievery.Patches
{
    [HarmonyPatch(typeof(GenDungeons), "generateDungeonPartial")]
    public static class GenDungeons_generateDungeonPartial_Patch
    {
        private static readonly AccessTools.FieldRef<GenDungeons, ICoreServerAPI> sapiRef =
            AccessTools.FieldRefAccess<GenDungeons, ICoreServerAPI>("sapi");

        private static readonly AccessTools.FieldRef<GenDungeons, IWorldGenBlockAccessor> worldGenAccessorRef =
            AccessTools.FieldRefAccess<GenDungeons, IWorldGenBlockAccessor>("worldgenBlockAccessor");

        [HarmonyPostfix]
        public static void Postfix(
            GenDungeons __instance,
            DungeonPlaceTask dungeonPlaceTask,
            IServerChunk[] chunks,
            int chunkX,
            int chunkZ)
        {
            if (dungeonPlaceTask == null || chunks == null || chunks.Length == 0) return;

            ICoreServerAPI sapi = sapiRef(__instance);
            IWorldGenBlockAccessor blockAccessor = worldGenAccessorRef(__instance);
            var api = sapi as ICoreAPI;

            if (api == null || blockAccessor == null) return;
            if (!WorldGenLockHelper.CanProcess(api, blockAccessor, out var worldGen)) return;
            if (dungeonPlaceTask.TilePlaceTasks == null || dungeonPlaceTask.TilePlaceTasks.Count == 0) return;

            BlockPos start = dungeonPlaceTask.TilePlaceTasks[0].Pos;
            string dungeonLockUid = $"dungeonlock_{dungeonPlaceTask.Code}_{start.X}_{start.Y}_{start.Z}";

            if (dungeonPlaceTask.SurfacePlaceTasks != null && dungeonPlaceTask.SurfacePlaceTasks.Count > 0)
            {
                WorldGenLockHelper.RegisterSurfaceTasks(dungeonPlaceTask.SurfacePlaceTasks, dungeonLockUid);
            }

            int minX = chunkX * 32;
            int minZ = chunkZ * 32;

            var chunkBox = new Cuboidi(
                minX,
                0,
                minZ,
                minX + 32,
                api.World.BlockAccessor.MapSizeY,
                minZ + 32
            );

            Cuboidi area = WorldGenLockHelper.Intersect(dungeonPlaceTask.DungeonBoundaries, chunkBox);
            if (area == null) return;

            Random rand = WorldGenLockHelper.CreateDeterministicRandom(api.World.Seed, start.X, start.Y, start.Z);
            WorldGenLockHelper.ProcessArea(api, blockAccessor, worldGen, area, dungeonLockUid, rand);
        }
    }
}