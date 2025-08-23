using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Thievery.Config;
using Thievery.Config.SubConfigs;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace Thievery.LockAndKey
{
    public static class WorldgenLockUtils
    {
        public const string WorldgenReinfUID = "010100110100111101010011";
        public enum LootTier { Easy, Medium, Hard, Brutal }
        public static bool IsWorldgenLockAt(IWorldAccessor world, BlockPos pos, string lockUidFromBE)
        {
            if (!string.IsNullOrEmpty(lockUidFromBE) && lockUidFromBE.StartsWith("structlock_", StringComparison.Ordinal))
                return true;
            var bawg = world.BlockAccessor as BlockAccessorWorldGen;
            int chunkX = pos.X >> 5, chunkY = pos.Y >> 5, chunkZ = pos.Z >> 5;
            var chunk = world.BlockAccessor.GetChunk(chunkX, chunkY, chunkZ) as IWorldChunk;
            if (chunk == null) return false;

            var reinfs = chunk.GetModdata<Dictionary<int, BlockReinforcement>>("reinforcements");
            if (reinfs == null) return false;

            int localX = pos.X & 31;
            int localY = pos.Y & 31;
            int localZ = pos.Z & 31;
            int localIndex = (localY << 16) | (localZ << 8) | localX;

            if (reinfs.TryGetValue(localIndex, out var bre))
            {
                if (bre != null && bre.Locked && bre.LastPlayername == WorldgenReinfUID)
                    return true;
                // Debug Loot
                /*else if (bre != null && bre.Locked && bre.LastPlayername == "Chronolegionaire")
                    return true;*/
            }
            return false;
        }
    }
    public static class WorldgenPickRewards
    {
        public static WorldgenLockUtils.LootTier TierFromDifficulty(int diff)
        {
            if (diff < 25) return WorldgenLockUtils.LootTier.Easy;
            if (diff < 50) return WorldgenLockUtils.LootTier.Medium;
            if (diff < 75) return WorldgenLockUtils.LootTier.Hard;
            return WorldgenLockUtils.LootTier.Brutal;
        }

        private static TierLootConfig TierConfigFor(WorldgenLockUtils.LootTier tier, LockpickRewardsConfig r)
            => tier switch
            {
                WorldgenLockUtils.LootTier.Easy   => r.Easy,
                WorldgenLockUtils.LootTier.Medium => r.Medium,
                WorldgenLockUtils.LootTier.Hard   => r.Hard,
                _                                 => r.Brutal
            };

        public static List<ItemStack> RollLoot(ICoreAPI api, int difficulty, Random rng, IServerPlayer player)
        {
            var rewards = ModConfig.Instance?.Rewards;
            var items = new List<ItemStack>();
            if (rewards == null || rewards.Enabled == false) return items;

            var tier    = TierFromDifficulty(difficulty);
            var tierCfg = TierConfigFor(tier, rewards);

            int emptyWeight = tierCfg.EmptyWeight;
            
            var pool = tierCfg.Pool;
            if (pool == null || pool.Count == 0 || tierCfg.Rolls <= 0) return items;

            for (int i = 0; i < tierCfg.Rolls; i++)
            {
                int totalW = emptyWeight;
                foreach (var e in pool) totalW += Math.Max(0, e.Weight);
                if (totalW <= 0) break;

                int pick = rng.Next(1, totalW + 1);
                if (pick <= emptyWeight) continue;

                int cum = emptyWeight;
                foreach (var e in pool)
                {
                    int w = Math.Max(0, e.Weight);
                    if (w == 0) continue;
                    cum += w;
                    if (pick > cum) continue;

                    var coll = LootWildcard.Resolve(api, e.Code, rng);
                    if (coll == null) break;

                    int min = Math.Min(e.Min, e.Max);
                    int max = Math.Max(e.Min, e.Max);
                    int q   = rng.Next(min, max + 1);
                    if (q <= 0) break;

                    items.Add(new ItemStack(coll, q));
                    break;
                }
            }
            return items;
        }

        public static int RustyGearsForDifficulty(int difficulty, Random rng)
        {
            var rewards = ModConfig.Instance?.Rewards;
            if (rewards == null || rewards.Enabled == false) return 0;

            var tier    = TierFromDifficulty(difficulty);
            var tierCfg = TierConfigFor(tier, rewards);

            int min = Math.Min(tierCfg.GearsMin, tierCfg.GearsMax);
            int max = Math.Max(tierCfg.GearsMin, tierCfg.GearsMax);
            if (max <= 0) return 0;

            return rng.Next(min, max + 1);
        }
        static class LootWildcard
        {
            private static readonly Dictionary<string, List<CollectibleObject>> cache = new();

            public static CollectibleObject Resolve(ICoreAPI api, string codeOrPattern, Random rng)
            {
                if (string.IsNullOrWhiteSpace(codeOrPattern)) return null;

                var loc    = AssetLocation.Create(codeOrPattern);
                var domain = string.IsNullOrEmpty(loc.Domain) ? "game" : loc.Domain;
                var path   = loc.Path ?? "";
                bool hasWildcard = path.IndexOfAny(new[] { '*', '?' }) >= 0;

                if (!hasWildcard)
                {
                    return (CollectibleObject)api.World.GetItem(new AssetLocation(domain, path))
                           ?? api.World.GetBlock(new AssetLocation(domain, path));
                }

                if (!cache.TryGetValue(codeOrPattern, out var list))
                {
                    list = new List<CollectibleObject>();
                    var rx = new Regex("^" + Regex.Escape(path).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                        RegexOptions.CultureInvariant);

                    foreach (var it in api.World.Items)
                        if (it?.Code != null && it.Code.Domain == domain && rx.IsMatch(it.Code.Path)) list.Add(it);
                    foreach (var bl in api.World.Blocks)
                        if (bl?.Code != null && bl.Code.Domain == domain && rx.IsMatch(bl.Code.Path)) list.Add(bl);

                    cache[codeOrPattern] = list;
                }

                if (list.Count == 0) return null;
                return list[rng.Next(list.Count)];
            }
        }
    }
}
