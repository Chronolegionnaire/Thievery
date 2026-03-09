using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace Thievery.Patches
{
    [HarmonyPatch(typeof(GenDungeons), "generateDungeonSurfacePartial")]
    public static class GenDungeons_generateDungeonSurfacePartial_Patch
    {
        private static readonly AccessTools.FieldRef<GenDungeons, ICoreServerAPI> sapiRef =
            AccessTools.FieldRefAccess<GenDungeons, ICoreServerAPI>("sapi");

        private static readonly AccessTools.FieldRef<GenDungeons, IWorldGenBlockAccessor> worldGenAccessorRef =
            AccessTools.FieldRefAccess<GenDungeons, IWorldGenBlockAccessor>("worldgenBlockAccessor");

        [HarmonyPostfix]
        public static void Postfix(
            GenDungeons __instance,
            List<TilePlaceTask> placeTasks,
            IServerChunk[] chunks,
            int chunkX,
            int chunkZ)
        {
            if (placeTasks == null || placeTasks.Count == 0 || chunks == null || chunks.Length == 0) return;

            ICoreServerAPI sapi = sapiRef(__instance);
            IWorldGenBlockAccessor blockAccessor = worldGenAccessorRef(__instance);
            var api = sapi as ICoreAPI;

            if (api == null || blockAccessor == null) return;
            if (!WorldGenLockHelper.CanProcess(api, blockAccessor, out var worldGen)) return;

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

            string dungeonLockUid = WorldGenLockHelper.ResolveSurfaceLockUid(placeTasks);

            if (string.IsNullOrEmpty(dungeonLockUid))
            {
                BlockPos start = placeTasks[0].Pos;
                dungeonLockUid = $"dungeonlocksurface_{start.X}_{start.Y}_{start.Z}";
            }

            BlockPos randPos = placeTasks[0].Pos;
            Random rand = WorldGenLockHelper.CreateDeterministicRandom(api.World.Seed, randPos.X, randPos.Y, randPos.Z);

            foreach (var task in placeTasks)
            {
                var tileBox = new Cuboidi(
                    task.Pos.X,
                    task.Pos.Y,
                    task.Pos.Z,
                    task.Pos.X + task.SizeX,
                    task.Pos.Y + task.SizeY,
                    task.Pos.Z + task.SizeZ
                );

                Cuboidi area = WorldGenLockHelper.Intersect(tileBox, chunkBox);
                if (area == null) continue;

                WorldGenLockHelper.ProcessArea(api, blockAccessor, worldGen, area, dungeonLockUid, rand);
            }

            WorldGenLockHelper.MarkSurfaceChunkProcessed(placeTasks, chunkX, chunkZ);
        }
    }
}