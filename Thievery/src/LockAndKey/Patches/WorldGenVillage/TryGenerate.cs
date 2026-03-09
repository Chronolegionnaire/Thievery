using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace Thievery.Patches
{
    [HarmonyPatch(typeof(WorldGenVillage), nameof(WorldGenVillage.TryGenerate))]
    public static class WorldGenVillage_TryGenerate_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(
            [HarmonyArgument("didGenerateStructure")] ref object didGenerateStructure,
            out List<PlacedArea> __state)
        {
            var captured = new List<PlacedArea>();
            __state = captured;

            var original = didGenerateStructure as Delegate;
            if (original == null) return;

            var wrapper = new DidGenWrapper
            {
                Original = original,
                Captured = captured
            };

            var delType = original.GetType();
            var mi = typeof(DidGenWrapper).GetMethod(nameof(DidGenWrapper.Invoke));
            didGenerateStructure = Delegate.CreateDelegate(delType, wrapper, mi);
        }

        [HarmonyPostfix]
        public static void Postfix(
            bool __result,
            List<PlacedArea> __state,
            IBlockAccessor blockAccessor,
            IWorldAccessor worldForCollectibleResolve)
        {
            if (!__result || __state == null || __state.Count == 0) return;

            var api = worldForCollectibleResolve?.Api;
            if (api == null) return;

            if (!WorldGenLockHelper.CanProcess(api, blockAccessor, out var worldGen)) return;

            foreach (var area in __state)
            {
                var rand = WorldGenLockHelper.CreateDeterministicRandom(api.World.Seed, area.X, area.Y, area.Z);
                string structureLockUid = $"villagelock_{area.X}_{area.Y}_{area.Z}";

                var bounds = new Cuboidi(
                    area.X,
                    area.Y,
                    area.Z,
                    area.X + area.SX,
                    area.Y + area.SY,
                    area.Z + area.SZ
                );

                WorldGenLockHelper.ProcessArea(api, blockAccessor, worldGen, bounds, structureLockUid, rand);
            }
        }

        public struct PlacedArea
        {
            public int X, Y, Z;
            public int SX, SY, SZ;
        }

        public sealed class DidGenWrapper
        {
            public Delegate Original;
            public List<PlacedArea> Captured;

            public void Invoke(Cuboidi location, BlockSchematicStructure structure)
            {
                BlockPos start = location?.Start?.AsBlockPos ?? new BlockPos(location.MinX, location.MinY, location.MinZ);

                Captured.Add(new PlacedArea
                {
                    X = start.X,
                    Y = start.Y,
                    Z = start.Z,
                    SX = structure.SizeX,
                    SY = structure.SizeY,
                    SZ = structure.SizeZ
                });

                Original?.DynamicInvoke(location, structure);
            }
        }
    }
}