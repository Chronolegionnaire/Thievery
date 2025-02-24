﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using Thievery.LockAndKey;
using Thievery.Config;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using Vintagestory.Server;

namespace Thievery.Patches
{
    [HarmonyPatch(typeof(WorldGenStructure), "TryGenerate")]
    public static class WorldGenStructure_TryGenerate_Patch
    {
        private static readonly HashSet<string> TargetBlockCodes = new HashSet<string>
        {
            "game:door*",
            "game:irondoor*",
            "game:chest*",
            "game:displaycase*",
            "game:displaycase-tall*",
            "game:trapdoor*",
            "game:chest-trunk*",
            "game:storagevessel*",
        };

        [HarmonyPostfix]
        public static void Postfix(
            WorldGenStructure __instance,
            bool __result,
            IBlockAccessor blockAccessor,
            IWorldAccessor worldForCollectibleResolve)
        {
            if (!__result || __instance.LastPlacedSchematicLocation == null)
                return;

            var api = worldForCollectibleResolve.Api;
            var modSystem = api.ModLoader.GetModSystem<ThieveryModSystem>();
            var config = ThieveryModSystem.LoadedConfig;
            var lockManager = modSystem?.LockManager;
            var reinforcementSystem = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();

            if (lockManager == null || reinforcementSystem == null || config == null)
            {
                return;
            }

            var blockAccessorWorldGen = blockAccessor as BlockAccessorWorldGen;
            if (blockAccessorWorldGen == null)
            {
                return;
            }
            Cuboidi location = __instance.LastPlacedSchematicLocation;
            Random rand = CreateDeterministicRandom(api.World.Seed, location.MinX, location.MinY, location.MinZ);

            string[] padlockTypes = new string[]
            {
                "game:padlock-bismuthbronze",
                "game:padlock-tinbronze",
                "game:padlock-blackbronze",
                "game:padlock-iron",
                "game:padlock-meteoriciron",
                "game:padlock-steel",
                "game:padlock-copper",
                "game:padlock-nickel",
                "game:padlock-silver",
                "game:padlock-gold",
                "game:padlock-titanium",
                "game:padlock-lead",
                "game:padlock-zinc",
                "game:padlock-tin",
                "game:padlock-chromium",
                "game:padlock-cupronickel",
                "game:padlock-electrum",
                "game:padlock-platinum"
            };

            int lockedCount = 0;
            int totalCount = 0;
            int reinforcedCount = 0;

            for (int x = location.MinX; x <= location.MaxX; x++)
            {
                for (int y = location.MinY; y <= location.MaxY; y++)
                {
                    for (int z = location.MinZ; z <= location.MaxZ; z++)
                    {
                        totalCount++;
                        BlockPos pos = new BlockPos(x, y, z);
                        Block block = blockAccessor.GetBlock(pos);
                        string blockCode = block.Code.ToString();
                        if (MatchesTarget(blockCode))
                        {
                            if (blockCode.StartsWith("game:chest"))
                            {
                                BlockEntity be = blockAccessor.GetBlockEntity(pos);
                                BlockEntityGenericTypedContainer chestEntity = be as BlockEntityGenericTypedContainer;
                                if (chestEntity != null)
                                {
                                    string chestType = chestEntity.type;
                                    if (chestType != null && chestType.StartsWith("collapsed"))
                                    {
                                        continue;
                                    }
                                }
                            }


                            bool hasReinforcementBehavior = block.HasBehavior<BlockBehaviorReinforcable>(false);
                            if (hasReinforcementBehavior)
                            {
                                double roll = rand.NextDouble();
                                if (roll < config.StructureLockChance)
                                {
                                    int strength = rand.Next(
                                        config.StructureMinReinforcement,
                                        config.StructureMaxReinforcement + 1
                                    );

                                    int chunkX = pos.X >> 5;
                                    int chunkY = pos.Y >> 5;
                                    int chunkZ = pos.Z >> 5;
                                    IWorldChunk chunk = blockAccessorWorldGen.GetChunk(chunkX, chunkY, chunkZ);
                                    if (chunk == null)
                                    {
                                        continue;
                                    }

                                    var reinforcements =
                                        chunk.GetModdata<Dictionary<int, BlockReinforcement>>("reinforcements")
                                        ?? new Dictionary<int, BlockReinforcement>();

                                    int localX = pos.X & 31;
                                    int localY = pos.Y & 31;
                                    int localZ = pos.Z & 31;
                                    int localIndex = (localY << 16) | (localZ << 8) | localX;
                                    string selectedPadlock = GetWeightedPadlock(rand, padlockTypes, config);

                                    var bre = new BlockReinforcement
                                    {
                                        PlayerUID = "010100110100111101010011",
                                        GroupUid = 0,
                                        LastPlayername = "someone a long time ago",
                                        LastGroupname = "someone a long time ago",
                                        Strength = strength,
                                        Locked = true,
                                        LockedByItemCode = selectedPadlock
                                    };

                                    reinforcements[localIndex] = bre;
                                    byte[] serializedData = SerializerUtil.Serialize(reinforcements);
                                    chunk.SetModdata("reinforcements", serializedData);
                                    chunk.MarkModified();
                                    var blockEntity = blockAccessor.GetBlockEntity(pos);
                                    if (blockEntity == null && blockAccessor is BlockAccessorWorldGen worldGenAccessor)
                                    {
                                        worldGenAccessor.SpawnBlockEntity("Generic", pos);
                                        blockEntity = blockAccessor.GetBlockEntity(pos);
                                    }

                                    if (blockEntity != null)
                                    {
                                        var lockBehavior = blockEntity.GetBehavior<BlockEntityThieveryLockData>();
                                        if (lockBehavior != null)
                                        {
                                            var lockUid = $"structlock_{pos.X}_{pos.Y}_{pos.Z}";
                                            bool isLocked = true;
                                            lockBehavior.LockUID = lockUid;
                                            lockBehavior.LockedState = isLocked;
                                            lockBehavior.LockType = selectedPadlock;
                                            blockEntity.MarkDirty(true);
                                        }
                                        else
                                        {
                                            api.Logger.Warning(
                                                $"[Thievery] BlockEntityThieveryLockData missing for {blockCode} at {pos}."
                                            );
                                        }
                                    }
                                    else
                                    {
                                        api.Logger.Warning(
                                            $"[Thievery] Failed to spawn block entity for {blockCode} at {pos}."
                                        );
                                    }
                                    lockedCount++;
                                }
                            }
                        }
                        else if (config.ReinforcedBuildingBlocks && MatchesReinforcedBuildingBlock(blockCode))
                        {
                            bool hasReinforcementBehavior = block.HasBehavior<BlockBehaviorReinforcable>(false);
                            if (!hasReinforcementBehavior)
                            {
                                continue;
                            }

                            int strength = rand.Next(
                                config.StructureMinReinforcement,
                                config.StructureMaxReinforcement + 1
                            );

                            int chunkX = pos.X >> 5;
                            int chunkY = pos.Y >> 5;
                            int chunkZ = pos.Z >> 5;
                            IWorldChunk chunk = blockAccessorWorldGen.GetChunk(chunkX, chunkY, chunkZ);
                            if (chunk == null)
                            {
                                continue;
                            }

                            var reinforcements =
                                chunk.GetModdata<Dictionary<int, BlockReinforcement>>("reinforcements")
                                ?? new Dictionary<int, BlockReinforcement>();

                            int localX = pos.X & 31;
                            int localY = pos.Y & 31;
                            int localZ = pos.Z & 31;
                            int localIndex = (localY << 16) | (localZ << 8) | localX;

                            var bre = new BlockReinforcement
                            {
                                PlayerUID = "010100110100111101010011",
                                GroupUid = 0,
                                LastPlayername = "someone a long time ago",
                                LastGroupname = "someone a long time ago",
                                Strength = strength,
                                Locked = false,
                                LockedByItemCode = ""
                            };

                            reinforcements[localIndex] = bre;
                            byte[] serializedData = SerializerUtil.Serialize(reinforcements);
                            chunk.SetModdata("reinforcements", serializedData);
                            chunk.MarkModified();

                            reinforcedCount++;
                        }
                    }
                }
            }
        }

        private static bool MatchesTarget(string code)
        {
            foreach (var target in TargetBlockCodes)
            {
                if (target.EndsWith("*"))
                {
                    if (code.StartsWith(target.TrimEnd('*')))
                    {
                        return true;
                    }
                }
                else if (code == target)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool MatchesReinforcedBuildingBlock(string code)
        {
            if (code.StartsWith("game:cobblestone"))
                return true;
            if (code.StartsWith("game:drystone"))
                return true;
            if (code.StartsWith("game:stonebrick"))
                return true;
            if (code.StartsWith("game:microblock"))
                return true;
            if (code.StartsWith("game:planks") && !code.StartsWith("game:planks-aged"))
                return true;
            if (code.StartsWith("game:polishedrock") && !code.StartsWith("game:polishedrockold"))
                return true;
            if (code.StartsWith("game:debarkedlog") &&
                !(code.EndsWith("-aged") || code.EndsWith("-veryaged") || code.EndsWith("-veryagedrotten")))
                return true;
            if (code.StartsWith("game:log-placed") && !code.Contains("-aged"))
                return true;

            return false;
        }

        private static Random CreateDeterministicRandom(long worldSeed, int posX, int posY, int posZ)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + worldSeed.GetHashCode();
                hash = hash * 31 + posX;
                hash = hash * 31 + posY;
                hash = hash * 31 + posZ;
                return new Random(hash);
            }
        }

        private static string GetWeightedPadlock(Random rand, string[] padlockTypes, Config.Config config)
        {
            float maxDuration = 0;
            Dictionary<string, float> weights = new Dictionary<string, float>();
            foreach (string padlock in padlockTypes)
            {
                float duration = GetPadlockDuration(padlock, config);
                if (duration > maxDuration)
                    maxDuration = duration;
            }
            foreach (string padlock in padlockTypes)
            {
                float duration = GetPadlockDuration(padlock, config);
                weights[padlock] = maxDuration / duration;
            }
            float totalWeight = 0;
            foreach (var weight in weights.Values)
            {
                totalWeight += weight;
            }
            float roll = (float)(rand.NextDouble() * totalWeight);
            float cumulative = 0;
            foreach (var kvp in weights)
            {
                cumulative += kvp.Value;
                if (roll < cumulative)
                {
                    return kvp.Key;
                }
            }
            return padlockTypes[0];
        }

        private static float GetPadlockDuration(string padlockCode, Config.Config config)
        {
            switch (padlockCode)
            {
                case "game:padlock-blackbronze":
                    return config.BlackBronzePadlockPickDurationSeconds;
                case "game:padlock-bismuthbronze":
                    return config.BismuthBronzePadlockPickDurationSeconds;
                case "game:padlock-tinbronze":
                    return config.TinBronzePadlockPickDurationSeconds;
                case "game:padlock-iron":
                    return config.IronPadlockPickDurationSeconds;
                case "game:padlock-meteoriciron":
                    return config.MeteoricIronPadlockPickDurationSeconds;
                case "game:padlock-steel":
                    return config.SteelPadlockPickDurationSeconds;
                case "game:padlock-copper":
                    return config.CopperPadlockPickDurationSeconds;
                case "game:padlock-nickel":
                    return config.NickelPadlockPickDurationSeconds;
                case "game:padlock-silver":
                    return config.SilverPadlockPickDurationSeconds;
                case "game:padlock-gold":
                    return config.GoldPadlockPickDurationSeconds;
                case "game:padlock-titanium":
                    return config.TitaniumPadlockPickDurationSeconds;
                case "game:padlock-lead":
                    return config.LeadPadlockPickDurationSeconds;
                case "game:padlock-zinc":
                    return config.ZincPadlockPickDurationSeconds;
                case "game:padlock-tin":
                    return config.TinPadlockPickDurationSeconds;
                case "game:padlock-chromium":
                    return config.ChromiumPadlockPickDurationSeconds;
                case "game:padlock-cupronickel":
                    return config.CupronickelPadlockPickDurationSeconds;
                case "game:padlock-electrum":
                    return config.ElectrumPadlockPickDurationSeconds;
                case "game:padlock-platinum":
                    return config.PlatinumPadlockPickDurationSeconds;
                default:
                    return 60;
            }
        }
    }
}
