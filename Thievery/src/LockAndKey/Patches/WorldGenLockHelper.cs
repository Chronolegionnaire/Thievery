using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Thievery.Config;
using Thievery.LockAndKey;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.Server;
using Vintagestory.ServerMods;

namespace Thievery.Patches
{
    internal static class WorldGenLockHelper
    {
        private sealed class SurfaceRegistration
        {
            public string LockUid;
            public List<TilePlaceTask> Tasks;
            public HashSet<long> PendingChunkColumns = new HashSet<long>();
        }

        private static readonly ConcurrentDictionary<string, string> SurfaceTaskToLockUid =
            new ConcurrentDictionary<string, string>();

        private static readonly ConcurrentDictionary<string, SurfaceRegistration> SurfaceRegistrations =
            new ConcurrentDictionary<string, SurfaceRegistration>();

        private static readonly HashSet<string> GlobalProcessedContainerIds = new HashSet<string>();

        internal static readonly HashSet<string> TargetBlockCodes = new HashSet<string>
        {
            "game:door*",
            "game:irondoor*",
            "game:chest*",
            "game:trapdoor*",
            "game:chest-trunk*",
            "game:woodenfencegate*"
        };

        internal static readonly string[] KeyNameLangKeys =
        {
            "thievery:keyname-forgotten",
            "thievery:keyname-discarded",
            "thievery:keyname-worn",
            "thievery:keyname-ancient",
            "thievery:keyname-weathered",
            "thievery:keyname-rusty",
            "thievery:keyname-tattered",
            "thievery:keyname-faded",
            "thievery:keyname-dilapidated",
            "thievery:keyname-obsolete",
            "thievery:keyname-aged",
            "thievery:keyname-oldrusty",
            "thievery:keyname-forgottenancient",
            "thievery:keyname-wornweathered",
            "thievery:keyname-abandonedrusty",
            "thievery:keyname-crumblingold",
            "thievery:keyname-decayedrusty",
            "thievery:keyname-mysteriousworn",
            "thievery:keyname-shatteredold",
            "thievery:keyname-agediron"
        };

        internal static readonly string[] PadlockTypes =
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

        public static string GetTileTaskKey(TilePlaceTask task)
        {
            if (task == null || task.Pos == null) return null;
            return $"{task.Pos.X}:{task.Pos.Y}:{task.Pos.Z}:{task.FileName?.ToString() ?? ""}:{task.Rotation}";
        }

        public static string GetSurfaceRegistrationKey(List<TilePlaceTask> surfaceTasks)
        {
            if (surfaceTasks == null || surfaceTasks.Count == 0) return null;

            var ordered = surfaceTasks
                .Select(GetTileTaskKey)
                .Where(k => k != null)
                .OrderBy(k => k, StringComparer.Ordinal);

            return string.Join("|", ordered);
        }

        private static long ChunkIndex2D(int chunkX, int chunkZ)
        {
            return ((long)chunkX << 32) ^ (uint)chunkZ;
        }

        private static HashSet<long> GetCoveredChunkColumns(List<TilePlaceTask> surfaceTasks)
        {
            var result = new HashSet<long>();
            if (surfaceTasks == null) return result;

            foreach (var task in surfaceTasks)
            {
                if (task?.Pos == null) continue;

                int minChunkX = task.Pos.X >> 5;
                int maxChunkX = (task.Pos.X + task.SizeX - 1) >> 5;
                int minChunkZ = task.Pos.Z >> 5;
                int maxChunkZ = (task.Pos.Z + task.SizeZ - 1) >> 5;

                for (int cx = minChunkX; cx <= maxChunkX; cx++)
                {
                    for (int cz = minChunkZ; cz <= maxChunkZ; cz++)
                    {
                        result.Add(ChunkIndex2D(cx, cz));
                    }
                }
            }

            return result;
        }

        public static void RegisterSurfaceTasks(List<TilePlaceTask> surfaceTasks, string lockUid)
        {
            if (surfaceTasks == null || surfaceTasks.Count == 0 || string.IsNullOrEmpty(lockUid)) return;

            string regKey = GetSurfaceRegistrationKey(surfaceTasks);
            if (regKey == null) return;

            var registration = SurfaceRegistrations.GetOrAdd(regKey, _ => new SurfaceRegistration
            {
                LockUid = lockUid,
                Tasks = new List<TilePlaceTask>(surfaceTasks),
                PendingChunkColumns = GetCoveredChunkColumns(surfaceTasks)
            });

            registration.LockUid = lockUid;

            foreach (var task in surfaceTasks)
            {
                string key = GetTileTaskKey(task);
                if (key == null) continue;
                SurfaceTaskToLockUid[key] = lockUid;
            }
        }

        public static void UnregisterSurfaceTasks(List<TilePlaceTask> surfaceTasks)
        {
            if (surfaceTasks == null || surfaceTasks.Count == 0) return;

            foreach (var task in surfaceTasks)
            {
                string key = GetTileTaskKey(task);
                if (key == null) continue;
                SurfaceTaskToLockUid.TryRemove(key, out _);
            }

            string regKey = GetSurfaceRegistrationKey(surfaceTasks);
            if (regKey != null)
            {
                SurfaceRegistrations.TryRemove(regKey, out _);
            }
        }

        public static string ResolveSurfaceLockUid(List<TilePlaceTask> surfaceTasks)
        {
            if (surfaceTasks == null) return null;

            foreach (var task in surfaceTasks)
            {
                string key = GetTileTaskKey(task);
                if (key == null) continue;

                if (SurfaceTaskToLockUid.TryGetValue(key, out var lockUid))
                {
                    return lockUid;
                }
            }

            return null;
        }

        public static void MarkSurfaceChunkProcessed(List<TilePlaceTask> surfaceTasks, int chunkX, int chunkZ)
        {
            if (surfaceTasks == null || surfaceTasks.Count == 0) return;

            string regKey = GetSurfaceRegistrationKey(surfaceTasks);
            if (regKey == null) return;

            if (!SurfaceRegistrations.TryGetValue(regKey, out var registration)) return;

            registration.PendingChunkColumns.Remove(ChunkIndex2D(chunkX, chunkZ));

            if (registration.PendingChunkColumns.Count == 0)
            {
                UnregisterSurfaceTasks(registration.Tasks);
            }
        }

        public static bool CanProcess(ICoreAPI api, IBlockAccessor blockAccessor, out BlockAccessorWorldGen worldGen)
        {
            worldGen = blockAccessor as BlockAccessorWorldGen;
            if (api == null || worldGen == null) return false;

            var modSystem = api.ModLoader.GetModSystem<ThieveryModSystem>();
            var lockManager = modSystem?.LockManager;
            var reinforcementSystem = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();

            return lockManager != null && reinforcementSystem != null;
        }

        public static void ProcessArea(
            ICoreAPI api,
            IBlockAccessor blockAccessor,
            BlockAccessorWorldGen worldGen,
            Cuboidi area,
            string lockUid,
            Random rand)
        {
            if (api == null || blockAccessor == null || worldGen == null || area == null || rand == null) return;

            int minX = Math.Min(area.X1, area.X2);
            int minY = Math.Min(area.Y1, area.Y2);
            int minZ = Math.Min(area.Z1, area.Z2);
            int maxX = Math.Max(area.X1, area.X2) - 1;
            int maxY = Math.Max(area.Y1, area.Y2) - 1;
            int maxZ = Math.Max(area.Z1, area.Z2) - 1;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        BlockPos pos = new BlockPos(x, y, z);
                        Block block = blockAccessor.GetBlock(pos);
                        if (block?.Code == null) continue;

                        string blockCode = block.Code.ToString();

                        if (MatchesTarget(blockCode))
                        {
                            bool isCollapsedChest = false;

                            if (blockCode.StartsWith("game:chest", StringComparison.Ordinal))
                            {
                                var be = blockAccessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer;
                                if (be?.type != null && be.type.StartsWith("collapsed", StringComparison.Ordinal))
                                {
                                    isCollapsedChest = true;
                                }
                            }

                            bool hasReinforcementBehavior = block.HasBehavior<BlockBehaviorReinforcable>(false);

                            if (hasReinforcementBehavior &&
                                rand.NextDouble() < ModConfig.Instance.WorldGen.StructureLockChance)
                            {
                                if (!isCollapsedChest)
                                {
                                    int strength = rand.Next(
                                        ModConfig.Instance.WorldGen.StructureMinReinforcement,
                                        ModConfig.Instance.WorldGen.StructureMaxReinforcement + 1
                                    );

                                    string selectedPadlock = GetWeightedPadlock(rand, PadlockTypes);

                                    if (TryWriteReinforcement(worldGen, pos, strength, true, selectedPadlock))
                                    {
                                        EnsureLockBehavior(api, worldGen, pos, lockUid, selectedPadlock);
                                    }
                                }
                            }

                            if (rand.NextDouble() < ModConfig.Instance.WorldGen.StructureKeyChance)
                            {
                                if (blockCode.StartsWith("game:chest", StringComparison.Ordinal) ||
                                    blockCode.StartsWith("game:trunk", StringComparison.Ordinal) ||
                                    blockCode.StartsWith("game:storagevessel", StringComparison.Ordinal) ||
                                    blockCode.StartsWith("game:stationarybasket", StringComparison.Ordinal) ||
                                    blockCode.StartsWith("game:groundstorage", StringComparison.Ordinal))
                                {
                                    InsertKeyIfMissing(api, blockAccessor, pos, lockUid, rand);
                                }
                            }
                        }
                        else if ((ModConfig.Instance.WorldGen.ReinforcedBuildingBlocks ||
                                  ModConfig.Instance.WorldGen.ReinforceAllBlocks) &&
                                 (ModConfig.Instance.WorldGen.ReinforceAllBlocks
                                     ? MatchesReinforcedBuildingBlockExtended(blockCode)
                                     : MatchesReinforcedBuildingBlock(blockCode)))
                        {
                            if (!block.HasBehavior<BlockBehaviorReinforcable>(false)) continue;

                            int strength = rand.Next(
                                ModConfig.Instance.WorldGen.StructureMinReinforcement,
                                ModConfig.Instance.WorldGen.StructureMaxReinforcement + 1
                            );

                            TryWriteReinforcement(worldGen, pos, strength, false, "");
                        }
                    }
                }
            }
        }

        public static bool MatchesTarget(string code)
        {
            foreach (var target in TargetBlockCodes)
            {
                if (target.EndsWith("*", StringComparison.Ordinal))
                {
                    if (code.StartsWith(target.TrimEnd('*'), StringComparison.Ordinal)) return true;
                }
                else if (code.Equals(target, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool MatchesReinforcedBuildingBlock(string code)
        {
            if (code.StartsWith("game:cobblestone", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:drystone", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:stonebrick", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:microblock", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:planks", StringComparison.Ordinal) && !code.StartsWith("game:planks-aged", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:polishedrock", StringComparison.Ordinal) && !code.StartsWith("game:polishedrockold", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:debarkedlog", StringComparison.Ordinal) && !(code.EndsWith("-aged", StringComparison.Ordinal) || code.EndsWith("-veryaged", StringComparison.Ordinal) || code.EndsWith("-veryagedrotten", StringComparison.Ordinal))) return true;
            if (code.StartsWith("game:log-placed", StringComparison.Ordinal) && !code.Contains("-aged")) return true;
            if (code.StartsWith("game:slantedroof", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:clayshingle", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:log-quad", StringComparison.Ordinal) && !code.Contains("-aged")) return true;
            if (code.StartsWith("game:plaster", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:glass", StringComparison.Ordinal) && !code.Contains("-vintage")) return true;
            if (code.StartsWith("game:chiseledblock", StringComparison.Ordinal)) return true;
            return false;
        }

        public static bool MatchesReinforcedBuildingBlockExtended(string code)
        {
            if (code.StartsWith("game:cobblestone", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:drystone", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:stonebrick", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:microblock", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:planks", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:polishedrock", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:debarkedlog", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:log-placed", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:cobbleskull", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:agedstonebrick", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:ironfence", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:slantedroof", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:clayshingle", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:log-quad", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:plaster", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:glass", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:brickruin", StringComparison.Ordinal)) return true;
            if (code.StartsWith("game:chiseledblock", StringComparison.Ordinal)) return true;
            return false;
        }

        public static Random CreateDeterministicRandom(long worldSeed, int posX, int posY, int posZ)
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

        public static string GetWeightedPadlock(Random rand, string[] padlockTypes)
        {
            float maxDuration = 0f;
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

            float totalWeight = 0f;
            foreach (float weight in weights.Values) totalWeight += weight;

            float roll = (float)(rand.NextDouble() * totalWeight);
            float cumulative = 0f;

            foreach (var kvp in weights)
            {
                cumulative += kvp.Value;
                if (roll < cumulative) return kvp.Key;
            }

            return padlockTypes[0];
        }

        public static float GetPadlockDuration(string padlockCode)
        {
            switch (padlockCode)
            {
                case "game:padlock-blackbronze": return ModConfig.Instance.Difficulty.BlackBronzePadlockDifficulty;
                case "game:padlock-bismuthbronze": return ModConfig.Instance.Difficulty.BismuthBronzePadlockDifficulty;
                case "game:padlock-tinbronze": return ModConfig.Instance.Difficulty.TinBronzePadlockDifficulty;
                case "game:padlock-iron": return ModConfig.Instance.Difficulty.IronPadlockDifficulty;
                case "game:padlock-meteoriciron": return ModConfig.Instance.Difficulty.MeteoricIronPadlockDifficulty;
                case "game:padlock-steel": return ModConfig.Instance.Difficulty.SteelPadlockDifficulty;
                case "game:padlock-copper": return ModConfig.Instance.Difficulty.CopperPadlockDifficulty;
                case "game:padlock-nickel": return ModConfig.Instance.Difficulty.NickelPadlockDifficulty;
                case "game:padlock-silver": return ModConfig.Instance.Difficulty.SilverPadlockDifficulty;
                case "game:padlock-gold": return ModConfig.Instance.Difficulty.GoldPadlockDifficulty;
                case "game:padlock-titanium": return ModConfig.Instance.Difficulty.TitaniumPadlockDifficulty;
                case "game:padlock-lead": return ModConfig.Instance.Difficulty.LeadPadlockDifficulty;
                case "game:padlock-zinc": return ModConfig.Instance.Difficulty.ZincPadlockDifficulty;
                case "game:padlock-tin": return ModConfig.Instance.Difficulty.TinPadlockDifficulty;
                case "game:padlock-chromium": return ModConfig.Instance.Difficulty.ChromiumPadlockDifficulty;
                case "game:padlock-cupronickel": return ModConfig.Instance.Difficulty.CupronickelPadlockDifficulty;
                case "game:padlock-electrum": return ModConfig.Instance.Difficulty.ElectrumPadlockDifficulty;
                case "game:padlock-platinum": return ModConfig.Instance.Difficulty.PlatinumPadlockDifficulty;
                default: return 60f;
            }
        }

        public static bool TryWriteReinforcement(BlockAccessorWorldGen worldGen, BlockPos pos, int strength, bool locked, string padlock)
        {
            int chunkX = pos.X >> 5;
            int chunkY = pos.Y >> 5;
            int chunkZ = pos.Z >> 5;

            IWorldChunk chunk = worldGen.GetChunk(chunkX, chunkY, chunkZ);
            if (chunk == null) return false;

            var reinforcements =
                chunk.GetModdata<Dictionary<int, BlockReinforcement>>("reinforcements") ??
                new Dictionary<int, BlockReinforcement>();

            int localX = pos.X & 31;
            int localY = pos.Y & 31;
            int localZ = pos.Z & 31;
            int localIndex = (localY << 16) | (localZ << 8) | localX;

            reinforcements[localIndex] = new BlockReinforcement
            {
                PlayerUID = "010100110100111101010011",
                GroupUid = 0,
                LastPlayername = Lang.Get("thievery:someone-long-ago"),
                LastGroupname = Lang.Get("thievery:someone-long-ago"),
                Strength = strength,
                Locked = locked,
                LockedByItemCode = padlock ?? ""
            };

            chunk.SetModdata("reinforcements", SerializerUtil.Serialize(reinforcements));
            chunk.MarkModified();
            return true;
        }

        public static void EnsureLockBehavior(ICoreAPI api, BlockAccessorWorldGen worldGen, BlockPos pos, string lockUid, string padlock)
        {
            var be = worldGen.GetBlockEntity(pos);

            if (be == null)
            {
                worldGen.SpawnBlockEntity("Generic", pos);
                be = worldGen.GetBlockEntity(pos);
            }

            if (be == null) return;

            var lockBehavior = be.GetBehavior<BlockEntityThieveryLockData>();
            if (lockBehavior == null) return;

            lockBehavior.LockUID = lockUid;
            lockBehavior.LockedState = true;
            lockBehavior.LockType = padlock;
            be.MarkDirty(true);
        }

        public static void InsertKeyIfMissing(ICoreAPI api, IBlockAccessor accessor, BlockPos pos, string lockUid, Random rand)
        {
            var container = accessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer;
            if (container == null) return;

            string containerId = container.Pos.ToString();
            if (GlobalProcessedContainerIds.Contains(containerId)) return;

            bool keyExists = false;

            for (int i = 0; i < container.Inventory.Count; i++)
            {
                if (container.Inventory[i].Empty) continue;

                var stack = container.Inventory[i].Itemstack;
                if (stack?.Collectible?.Code?.Path?.Equals("thievery:key-aged", StringComparison.OrdinalIgnoreCase) == true &&
                    stack.Attributes.GetString("keyUID") == lockUid)
                {
                    keyExists = true;
                    break;
                }
            }

            if (keyExists) return;

            for (int i = 0; i < container.Inventory.Count; i++)
            {
                if (!container.Inventory[i].Empty) continue;

                Item keyItem = api.World.GetItem(new AssetLocation("thievery:key-aged"));
                if (keyItem == null) return;

                string nameKey = KeyNameLangKeys[rand.Next(KeyNameLangKeys.Length)];

                ItemStack stack = new ItemStack(keyItem);
                stack.Attributes.SetString("keyUID", lockUid);
                stack.Attributes.SetString("keyName", Lang.Get(nameKey));
                stack.Attributes.SetString("keyNameCode", nameKey);

                container.Inventory[i].Itemstack = stack;
                GlobalProcessedContainerIds.Add(containerId);
                return;
            }
        }

        public static Cuboidi Intersect(Cuboidi a, Cuboidi b)
        {
            int x1 = Math.Max(a.X1, b.X1);
            int y1 = Math.Max(a.Y1, b.Y1);
            int z1 = Math.Max(a.Z1, b.Z1);
            int x2 = Math.Min(a.X2, b.X2);
            int y2 = Math.Min(a.Y2, b.Y2);
            int z2 = Math.Min(a.Z2, b.Z2);

            if (x1 >= x2 || y1 >= y2 || z1 >= z2) return null;
            return new Cuboidi(x1, y1, z1, x2, y2, z2);
        }
    }
}