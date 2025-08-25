using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Thievery.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Thievery.LockAndKey
{
    public class BlockEntityThieveryLockData : BlockEntityBehavior
    {
        private string lockUID;
        private bool   lockedState;
        private string lockType;
        private static Dictionary<string, (string Display, PropertyInfo Prop)> lockTypeIndex;
        private long globalLockoutUntilMs;
        private readonly Dictionary<string, long> perPlayerLockoutUntilMs = new();

        public string LockUID     { get => lockUID;     set { lockUID = value;     Blockentity.MarkDirty(); } }
        public bool   LockedState { get => lockedState; set { lockedState = value; Blockentity.MarkDirty(); } }
        public string LockType    { get => lockType;    set { lockType = value;    Blockentity.MarkDirty(); } }
        
        public long LockpickLockoutUntilMs
        {
            get => globalLockoutUntilMs;
            set { globalLockoutUntilMs = value; Blockentity.MarkDirty(); }
        }

        private bool worldGenPickRewardGranted;
        public bool WorldGenPickRewardGranted
        {
            get => worldGenPickRewardGranted;
            set { worldGenPickRewardGranted = value; Blockentity.MarkDirty(); }
        }

        public BlockEntityThieveryLockData(BlockEntity be) : base(be) { }
        public bool IsInLockoutFor(IWorldAccessor world, string playerUid)
        {
            long now = world?.ElapsedMilliseconds ?? 0;

            if (globalLockoutUntilMs != 0)
            {
                if (globalLockoutUntilMs < 0) return true;
                if (now < globalLockoutUntilMs) return true;
            }

            if (string.IsNullOrEmpty(playerUid)) return false;
            if (perPlayerLockoutUntilMs.TryGetValue(playerUid, out long until))
            {
                if (until < 0) return true;
                if (now < until) return true;
            }
            return false;
        }

        public void ClearAllLockouts()
        {
            globalLockoutUntilMs = 0;
            perPlayerLockoutUntilMs.Clear();
            Blockentity.MarkDirty();
        }

        public void ClearPlayerLockout(string playerUid)
        {
            if (string.IsNullOrEmpty(playerUid)) return;
            if (perPlayerLockoutUntilMs.Remove(playerUid))
                Blockentity.MarkDirty();
        }

        public void SetGlobalLockout(long untilMs)
        {
            globalLockoutUntilMs = untilMs;
            Blockentity.MarkDirty();
        }

        public void SetPerPlayerLockout(string playerUid, long untilMs)
        {
            if (string.IsNullOrEmpty(playerUid)) return;
            perPlayerLockoutUntilMs[playerUid] = untilMs;
            Blockentity.MarkDirty();
        }


        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetString("lockUID", lockUID);
            tree.SetBool("lockedState", lockedState);
            tree.SetString("lockType", lockType);
            tree.SetLong("lockpickLockoutUntilMs", globalLockoutUntilMs);
            tree.SetLong("globalLockoutUntilMs", globalLockoutUntilMs);

            var per = new TreeAttribute();
            foreach (var kv in perPlayerLockoutUntilMs)
                per.SetLong(kv.Key, kv.Value);
            tree["perPlayerLockoutUntilMs"] = per;

            tree.SetBool("worldGenPickRewardGranted", worldGenPickRewardGranted);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);

            lockUID     = tree.GetString("lockUID", null);
            lockedState = tree.GetBool("lockedState", false);
            lockType    = tree.GetString("lockType", null);
            globalLockoutUntilMs = tree.GetLong("globalLockoutUntilMs", tree.GetLong("lockpickLockoutUntilMs", 0));
            
            if (globalLockoutUntilMs == -1 && (ModConfig.Instance?.MiniGame?.PermanentLockBreak != true))
            {
                long now   = worldForResolve?.ElapsedMilliseconds ?? 0;
                long mins  = Math.Max(1, ModConfig.Instance?.MiniGame?.ProbeLockoutMinutes ?? 10);
                globalLockoutUntilMs = now + mins * 60L * 1000L;
                Blockentity.MarkDirty();
            }

            perPlayerLockoutUntilMs.Clear();

            if (tree["perPlayerLockoutUntilMs"] is ITreeAttribute per)
            {
                foreach (var kv in per)
                {
                    long until = 0;
                    if (per.TryGetAttribute(kv.Key, out var attr) && attr is LongAttribute la) until = la.value;
                    else until = per.GetLong(kv.Key, 0);
                    perPlayerLockoutUntilMs[kv.Key] = until;
                }
            }

            worldGenPickRewardGranted = tree.GetBool("worldGenPickRewardGranted", false);
        }
        private int? TryGetPadlockDifficulty(string rawType)
        {
            var cfg = ModConfig.Instance?.Difficulty;
            if (cfg == null) return null;

            EnsureLockTypeIndex(cfg);

            string key = NormalizeKey(rawType);
            if (key == null) return null;

            if (lockTypeIndex != null && lockTypeIndex.TryGetValue(key, out var entry))
            {
                return (int)entry.Prop.GetValue(cfg);
            }

            return null;
        }

        private static string FormatLockType(string rawType)
        {
            var cfg = ModConfig.Instance?.Difficulty;
            if (cfg != null)
            {
                EnsureLockTypeIndex(cfg);
                string key = NormalizeKey(rawType);
                if (key != null && lockTypeIndex != null && lockTypeIndex.TryGetValue(key, out var entry))
                {
                    // Perfect, display straight from the PascalCase stem
                    return entry.Display;
                }
            }

            // Fallback: old behavior (handles things like "padlock-copper")
            if (string.IsNullOrWhiteSpace(rawType)) return null;
            string type = rawType;
            if (type.StartsWith("padlock-", StringComparison.OrdinalIgnoreCase))
                type = type.Substring("padlock-".Length);

            type = type.Replace("-", " ");
            var ti = CultureInfo.InvariantCulture.TextInfo;
            return ti.ToTitleCase(type);
        }

        private static string NormalizeKey(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            string s = raw.Trim();
            if (s.StartsWith("padlock-", StringComparison.OrdinalIgnoreCase))
                s = s.Substring("padlock-".Length);

            // remove spaces and hyphens, lowercase
            s = s.Replace("-", "").Replace(" ", "").ToLowerInvariant();
            return s;
        }

        /// <summary>Split a PascalCase stem like "BlackBronze" into "Black Bronze".</summary>
        private static string DisplayFromStem(string stem)
        {
            if (string.IsNullOrEmpty(stem)) return stem;
            // Insert spaces before capitals (but not at the very start)
            string spaced = Regex.Replace(stem, "(\\B[A-Z])", " $1");
            return spaced;
        }

        /// <summary>Build the index once from the config type.</summary>
        private static void EnsureLockTypeIndex(object difficultyConfig)
        {
            if (lockTypeIndex != null || difficultyConfig == null) return;

            lockTypeIndex = new Dictionary<string, (string Display, PropertyInfo Prop)>();

            var t = difficultyConfig.GetType();
            foreach (var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.PropertyType != typeof(int)) continue;
                if (!prop.Name.EndsWith("PadlockDifficulty", StringComparison.Ordinal)) continue;

                // Stem: e.g. "BlackBronze" from "BlackBronzePadlockDifficulty"
                string stem = prop.Name.Substring(0, prop.Name.Length - "PadlockDifficulty".Length);

                string display = DisplayFromStem(stem);              // "Black Bronze"
                string key     = NormalizeKey(stem);                 // "blackbronze"

                // Guard against duplicates; last one wins is fine here
                lockTypeIndex[key] = (display, prop);
            }
        }
        public override void GetBlockInfo(IPlayer forPlayer, System.Text.StringBuilder description)
        {
            base.GetBlockInfo(forPlayer, description);

            if (!string.IsNullOrWhiteSpace(lockUID))
            {
                string lockedText = lockedState ? Lang.Get("thievery:locked") : Lang.Get("thievery:unlocked");
                string stateLine  = Lang.Get("thievery:locked-state", lockedText);
                if (!description.ToString().Contains(stateLine))
                    description.AppendLine(stateLine);
            }

            if (!string.IsNullOrWhiteSpace(lockType))
            {
                string displayType = FormatLockType(lockType);
                if (!string.IsNullOrEmpty(displayType))
                {
                    description.AppendLine($"Lock Type: {displayType}");

                    var diff = TryGetPadlockDifficulty(lockType);
                    if (diff.HasValue)
                    {
                        description.AppendLine($"Difficulty: {diff.Value}");
                    }
                }
            }


            var world = Blockentity?.Api?.World;
            if (world == null) return;

            long now = world.ElapsedMilliseconds;

            bool perActive = false;
            long perUntil  = 0;
            if (forPlayer != null && perPlayerLockoutUntilMs.TryGetValue(forPlayer.PlayerUID, out var pUntil))
            {
                perActive = (pUntil == -1) || (pUntil > now);
                perUntil  = pUntil;
            }
            bool globalActive = (globalLockoutUntilMs == -1) || (globalLockoutUntilMs > now);

            if (!perActive && !globalActive) return;

            long showUntil = perActive ? perUntil : globalLockoutUntilMs;

            if (showUntil == -1)
            {
                description.AppendLine(Lang.Get("thievery:lock-broken"));
            }
            else
            {
                long remainingSec = (long)Math.Ceiling((showUntil - now) / 1000.0);
                description.AppendLine(Lang.Get("thievery:lock-broken-seconds", remainingSec));
            }
        }
    }
}
