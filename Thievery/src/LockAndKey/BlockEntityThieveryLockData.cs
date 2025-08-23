using System;
using System.Collections.Generic;
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
        
        public override void GetBlockInfo(IPlayer forPlayer, System.Text.StringBuilder description)
        {
            base.GetBlockInfo(forPlayer, description);

            if (!string.IsNullOrWhiteSpace(lockUID))
            {
                string lockedText = lockedState ? Lang.Get("thievery:locked") : Lang.Get("thievery:unlocked");
                string stateLine  = Lang.Get("thievery:locked-state", lockedText);
                if (!description.ToString().Contains(stateLine)) description.AppendLine(stateLine);
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
