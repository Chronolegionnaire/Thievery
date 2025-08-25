﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Thievery.Config;
using Thievery.LockAndKey;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using Vintagestory.Server;

namespace Thievery.Patches
{

    [HarmonyPatch(typeof(WorldGenVillage), nameof(WorldGenVillage.TryGenerate))]
    public static class WorldGenVillage_TryGenerate_Patch
    {
        private static readonly HashSet<string> GlobalProcessedContainerIds = new HashSet<string>();

        private static readonly HashSet<string> TargetBlockCodes = new HashSet<string>
        {
            "game:door*",
            "game:irondoor*",
            "game:chest*",
            "game:trapdoor*",
            "game:chest-trunk*",
            "game:woodenfencegate*"
        };

        private static readonly string[] KeyNameLangKeys = new string[]
        {
            "thievery:keyname-forgotten","thievery:keyname-discarded","thievery:keyname-worn","thievery:keyname-ancient",
            "thievery:keyname-weathered","thievery:keyname-rusty","thievery:keyname-tattered","thievery:keyname-faded",
            "thievery:keyname-dilapidated","thievery:keyname-obsolete","thievery:keyname-aged","thievery:keyname-oldrusty",
            "thievery:keyname-forgottenancient","thievery:keyname-wornweathered","thievery:keyname-abandonedrusty",
            "thievery:keyname-crumblingold","thievery:keyname-decayedrusty","thievery:keyname-mysteriousworn",
            "thievery:keyname-shatteredold","thievery:keyname-agediron"
        };

        private static readonly string[] PadlockTypes = new string[]
        {
            "game:padlock-bismuthbronze","game:padlock-tinbronze","game:padlock-blackbronze","game:padlock-iron",
            "game:padlock-meteoriciron","game:padlock-steel","game:padlock-copper","game:padlock-nickel",
            "game:padlock-silver","game:padlock-gold","game:padlock-titanium","game:padlock-lead","game:padlock-zinc",
            "game:padlock-tin","game:padlock-chromium","game:padlock-cupronickel","game:padlock-electrum","game:padlock-platinum"
        };

        [HarmonyPrefix]
        public static void Prefix(
            [HarmonyArgument("didGenerateStructure")] ref object didGenerateStructure,
            out List<PlacedArea> __state
        )
        {
            var captured = new List<PlacedArea>();
            __state = captured;

            var original = didGenerateStructure as Delegate;
            if (original == null) return;

            var wrapper = new DidGenWrapper { Original = original, Captured = captured };
            var delType = original.GetType();
            var mi = typeof(DidGenWrapper).GetMethod(nameof(DidGenWrapper.Invoke));
            didGenerateStructure = Delegate.CreateDelegate(delType, wrapper, mi);
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
                    X = start.X, Y = start.Y, Z = start.Z,
                    SX = structure.SizeX, SY = structure.SizeY, SZ = structure.SizeZ
                });

                Original?.DynamicInvoke(location, structure);
            }
        }

        [HarmonyPostfix]
        public static void Postfix(
            bool __result,
            List<PlacedArea> __state,
            IBlockAccessor blockAccessor,
            IWorldAccessor worldForCollectibleResolve
        )
        {
            if (!__result || __state == null || __state.Count == 0) return;

            var api = worldForCollectibleResolve.Api;
            var modSystem = api.ModLoader.GetModSystem<ThieveryModSystem>();
            var lockManager = modSystem?.LockManager;
            var reinforcementSystem = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
            var blockAccessorWorldGen = blockAccessor as BlockAccessorWorldGen;

            if (lockManager == null || reinforcementSystem == null || blockAccessorWorldGen == null) return;

            foreach (var area in __state)
            {
                var rand = CreateDeterministicRandom(api.World.Seed, area.X, area.Y, area.Z);
                string structureLockUid = $"villagelock_{area.X}_{area.Y}_{area.Z}";

                int minX = area.X, maxX = area.X + area.SX - 1;
                int minY = area.Y, maxY = area.Y + area.SY - 1;
                int minZ = area.Z, maxZ = area.Z + area.SZ - 1;

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        for (int z = minZ; z <= maxZ; z++)
                        {
                            BlockPos pos = new BlockPos(x, y, z);
                            Block block = blockAccessor.GetBlock(pos);
                            if (block == null || block.Code == null) continue;

                            string blockCode = block.Code.ToString();
                            if (MatchesTarget(blockCode))
                            {
                                bool isCollapsedChest = false;
                                if (blockCode.StartsWith("game:chest"))
                                {
                                    var be = blockAccessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer;
                                    if (be?.type != null && be.type.StartsWith("collapsed")) isCollapsedChest = true;
                                }

                                bool hasReinf = block.HasBehavior<BlockBehaviorReinforcable>(false);
                                if (!hasReinf) continue;

                                if (rand.NextDouble() < ModConfig.Instance.WorldGen.StructureLockChance)
                                {
                                    if (!isCollapsedChest)
                                    {
                                        int strength = rand.Next(
                                            ModConfig.Instance.WorldGen.StructureMinReinforcement,
                                            ModConfig.Instance.WorldGen.StructureMaxReinforcement + 1
                                        );

                                        var padlock = GetWeightedPadlock(rand, PadlockTypes);

                                        if (TryWriteReinforcement(blockAccessorWorldGen, pos, strength, locked: true,
                                                padlock))
                                        {
                                            EnsureLockBehavior(api, blockAccessorWorldGen, pos, structureLockUid,
                                                padlock);
                                        }
                                    }

                                    if (blockCode.StartsWith("game:chest-trunk")
                                        || blockCode.StartsWith("game:chest")
                                        || blockCode.StartsWith("game:storagevessel")
                                        || blockCode.StartsWith("game:groundstorage"))
                                    {
                                        InsertKeyIfMissing(api, blockAccessor, pos, structureLockUid, rand);
                                    }
                                }
                            }
                            else if ((ModConfig.Instance.WorldGen.ReinforcedBuildingBlocks ||
                                      ModConfig.Instance.WorldGen.ReinforceAllBlocks)
                                     && (ModConfig.Instance.WorldGen.ReinforceAllBlocks
                                         ? MatchesReinforcedBuildingBlockExtended(blockCode)
                                         : MatchesReinforcedBuildingBlock(blockCode)))
                            {
                                if (!block.HasBehavior<BlockBehaviorReinforcable>(false)) continue;

                                int strength = rand.Next(
                                    ModConfig.Instance.WorldGen.StructureMinReinforcement,
                                    ModConfig.Instance.WorldGen.StructureMaxReinforcement + 1
                                );

                                TryWriteReinforcement(blockAccessorWorldGen, pos, strength, locked: false, padlock: "");
                            }
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
                    if (code.StartsWith(target.TrimEnd('*'))) return true;
                }
                else if (code == target) return true;
            }
            return false;
        }

        private static bool MatchesReinforcedBuildingBlock(string code)
        {
            if (code.StartsWith("game:cobblestone")) return true;
            if (code.StartsWith("game:drystone")) return true;
            if (code.StartsWith("game:stonebrick")) return true;
            if (code.StartsWith("game:microblock")) return true;
            if (code.StartsWith("game:planks") && !code.StartsWith("game:planks-aged")) return true;
            if (code.StartsWith("game:polishedrock") && !code.StartsWith("game:polishedrockold")) return true;
            if (code.StartsWith("game:debarkedlog") && !(code.EndsWith("-aged") || code.EndsWith("-veryaged") || code.EndsWith("-veryagedrotten"))) return true;
            if (code.StartsWith("game:log-placed") && !code.Contains("-aged")) return true;
            if (code.StartsWith("game:slantedroof")) return true;
            if (code.StartsWith("game:clayshingle")) return true;
            if (code.StartsWith("game:microblock")) return true;
            if (code.StartsWith("game:log-quad") && !code.Contains("-aged")) return true;
            if (code.StartsWith("game:plaster")) return true;
            if (code.StartsWith("game:glass") && !code.Contains("-vintage")) return true;
            if (code.StartsWith("game:chiseledblock")) return true;
            return false;
        }

        private static bool MatchesReinforcedBuildingBlockExtended(string code)
        {
            if (code.StartsWith("game:cobblestone")) return true;
            if (code.StartsWith("game:drystone")) return true;
            if (code.StartsWith("game:stonebrick")) return true;
            if (code.StartsWith("game:microblock")) return true;
            if (code.StartsWith("game:planks")) return true;
            if (code.StartsWith("game:polishedrock")) return true;
            if (code.StartsWith("game:debarkedlog")) return true;
            if (code.StartsWith("game:log-placed")) return true;
            if (code.StartsWith("game:cobbleskull")) return true;
            if (code.StartsWith("game:agedstonebrick")) return true;
            if (code.StartsWith("game:ironfence")) return true;
            if (code.StartsWith("game:slantedroof")) return true;
            if (code.StartsWith("game:clayshingle")) return true;
            if (code.StartsWith("game:microblock")) return true;
            if (code.StartsWith("game:log-quad")) return true;
            if (code.StartsWith("game:plaster")) return true;
            if (code.StartsWith("game:glass")) return true;
            if (code.StartsWith("game:brickruin")) return true;
            if (code.StartsWith("game:chiseledblock")) return true;
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

        private static string GetWeightedPadlock(Random rand, string[] padlockTypes)
        {
            float maxDuration = 0;
            var weights = new Dictionary<string, float>();
            foreach (string padlock in padlockTypes)
            {
                float duration = GetPadlockDuration(padlock);
                if (duration > maxDuration) maxDuration = duration;
            }
            foreach (string padlock in padlockTypes)
            {
                float duration = GetPadlockDuration(padlock);
                weights[padlock] = maxDuration / duration;
            }
            float totalWeight = 0;
            foreach (var w in weights.Values) totalWeight += w;
            float roll = (float)(rand.NextDouble() * totalWeight);
            float cum = 0;
            foreach (var kv in weights)
            {
                cum += kv.Value;
                if (roll < cum) return kv.Key;
            }
            return padlockTypes[0];
        }

        private static float GetPadlockDuration(string padlockCode)
        {
            switch (padlockCode)
            {
                case "game:padlock-blackbronze":   return ModConfig.Instance.Difficulty.BlackBronzePadlockDifficulty;
                case "game:padlock-bismuthbronze": return ModConfig.Instance.Difficulty.BismuthBronzePadlockDifficulty;
                case "game:padlock-tinbronze":     return ModConfig.Instance.Difficulty.TinBronzePadlockDifficulty;
                case "game:padlock-iron":          return ModConfig.Instance.Difficulty.IronPadlockDifficulty;
                case "game:padlock-meteoriciron":  return ModConfig.Instance.Difficulty.MeteoricIronPadlockDifficulty;
                case "game:padlock-steel":         return ModConfig.Instance.Difficulty.SteelPadlockDifficulty;
                case "game:padlock-copper":        return ModConfig.Instance.Difficulty.CopperPadlockDifficulty;
                case "game:padlock-nickel":        return ModConfig.Instance.Difficulty.NickelPadlockDifficulty;
                case "game:padlock-silver":        return ModConfig.Instance.Difficulty.SilverPadlockDifficulty;
                case "game:padlock-gold":          return ModConfig.Instance.Difficulty.GoldPadlockDifficulty;
                case "game:padlock-titanium":      return ModConfig.Instance.Difficulty.TitaniumPadlockDifficulty;
                case "game:padlock-lead":          return ModConfig.Instance.Difficulty.LeadPadlockDifficulty;
                case "game:padlock-zinc":          return ModConfig.Instance.Difficulty.ZincPadlockDifficulty;
                case "game:padlock-tin":           return ModConfig.Instance.Difficulty.TinPadlockDifficulty;
                case "game:padlock-chromium":      return ModConfig.Instance.Difficulty.ChromiumPadlockDifficulty;
                case "game:padlock-cupronickel":   return ModConfig.Instance.Difficulty.CupronickelPadlockDifficulty;
                case "game:padlock-electrum":      return ModConfig.Instance.Difficulty.ElectrumPadlockDifficulty;
                case "game:padlock-platinum":      return ModConfig.Instance.Difficulty.PlatinumPadlockDifficulty;
                default:                           return 60;
            }
        }
        private static bool TryWriteReinforcement(BlockAccessorWorldGen worldGen, BlockPos pos, int strength, bool locked, string padlock)
        {
            int chunkX = pos.X >> 5;
            int chunkY = pos.Y >> 5;
            int chunkZ = pos.Z >> 5;
            var chunk = worldGen.GetChunk(chunkX, chunkY, chunkZ);
            if (chunk == null) return false;

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
                LastPlayername = Lang.Get("thievery:someone-long-ago"),
                LastGroupname = Lang.Get("thievery:someone-long-ago"),
                Strength = strength,
                Locked = locked,
                LockedByItemCode = padlock ?? ""
            };

            reinforcements[localIndex] = bre;
            chunk.SetModdata("reinforcements", SerializerUtil.Serialize(reinforcements));
            chunk.MarkModified();
            return true;
        }

        private static void EnsureLockBehavior(ICoreAPI api, BlockAccessorWorldGen worldGen, BlockPos pos, string lockUid, string padlock)
        {
            var be = worldGen.GetBlockEntity(pos);
            if (be == null)
            {
                worldGen.SpawnBlockEntity("Generic", pos);
                be = worldGen.GetBlockEntity(pos);
            }

            if (be != null)
            {
                var lockBehavior = be.GetBehavior<BlockEntityThieveryLockData>();
                if (lockBehavior != null)
                {
                    lockBehavior.LockUID = lockUid;
                    lockBehavior.LockedState = true;
                    lockBehavior.LockType = padlock;
                    be.MarkDirty(true);
                }
                else
                {
                    api.Logger.Warning($"[Thievery] BlockEntityThieveryLockData missing at {pos}.");
                }
            }
            else
            {
                api.Logger.Warning($"[Thievery] Failed to spawn block entity at {pos}.");
            }
        }

        private static void InsertKeyIfMissing(ICoreAPI api, IBlockAccessor accessor, BlockPos pos, string lockUid, Random rand)
        {
            var container = accessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer;
            if (container == null) return;

            string containerId = container.Pos.ToString();
            if (GlobalProcessedContainerIds.Contains(containerId)) return;

            bool keyExists = false;
            for (int i = 0; i < container.Inventory.Count; i++)
            {
                if (container.Inventory[i].Empty) continue;
                var st = container.Inventory[i].Itemstack;
                if (st?.Collectible?.Code?.Path?.Equals("thievery:key-aged", StringComparison.OrdinalIgnoreCase) == true
                    && st.Attributes.GetString("keyUID") == lockUid)
                {
                    keyExists = true; break;
                }
            }

            if (keyExists) return;

            for (int i = 0; i < container.Inventory.Count; i++)
            {
                if (!container.Inventory[i].Empty) continue;

                var keyItem = api.World.GetItem(new AssetLocation("thievery:key-aged"));
                if (keyItem == null) break;

                string nameKey = KeyNameLangKeys[rand.Next(KeyNameLangKeys.Length)];

                var stack = new ItemStack(keyItem);
                stack.Attributes.SetString("keyUID", lockUid);
                stack.Attributes.SetString("keyName", Lang.Get(nameKey));
                stack.Attributes.SetString("keyNameCode", nameKey);

                container.Inventory[i].Itemstack = stack;
                GlobalProcessedContainerIds.Add(containerId);
                break;
            }
        }
    }
}