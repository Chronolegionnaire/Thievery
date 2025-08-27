using System;
using System.Collections.Generic;
using Thievery.Config;
using Thievery.Config.SubConfigs;
using Thievery.LockAndKey;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Thievery.StickyFingers
{
    public class EntityBehaviorPickpocket : EntityBehavior
    {
        private float baseSuccessChance;
        private bool stealFullStack;
        private float barFillSeconds;
        float tickIntervalSec      = 0.05f;
        private float attemptCooldownSec = 5f;
        float attemptRange         = 1.75f;
        float cancelMaxDistance    = 2.25f;
        bool  mustStayBehind       = true;
        bool  requireHandsEmpty    = true;

        string[] inventoryOrder    = new[] { "hotbar", "backpack", "character" };
        string[] slotBlacklist     = Array.Empty<string>();

        ICoreAPI api;
        ICoreServerAPI sapi;
        IServerNetworkChannel net;
        double lastAttemptTotalSeconds = -9999;
        readonly Dictionary<string, PickSession> sessions = new();

        long tickListenerId = 0;
        readonly Random rnd = new Random();

        public EntityBehaviorPickpocket(Entity entity) : base(entity) { }
        public override string PropertyName() => "pickpocket";

        class PickSession
        {
            public IServerPlayer Thief;
            public EntityAgent ThiefAgent;
            public long StartMs;
            public long DurationMs;
            public bool Closed;
        }
        bool HasAnyRequiredTrait(IPlayer player, List<string> required)
        {
            if (required == null || required.Count == 0) return true;

            var characterSystem = api?.ModLoader?.GetModSystem<CharacterSystem>();
            if (characterSystem == null)
            {
                return false;
            }

            foreach (var trait in required)
            {
                if (characterSystem.HasTrait(player, trait)) return true;
            }
            return false;
        }
        bool _featureEnabled = true;
        List<string> _requiredTraits = new List<string>();
        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            api  = entity.Api;
            sapi = api as ICoreServerAPI;
            if (attributes != null)
            {
                tickIntervalSec    = attributes["tickIntervalSec"].AsFloat(tickIntervalSec);
                attemptCooldownSec = attributes["attemptCooldownSec"].AsFloat(attemptCooldownSec);
                attemptRange       = attributes["attemptRange"].AsFloat(attemptRange);
                baseSuccessChance  = attributes["baseSuccessChance"].AsFloat(baseSuccessChance);
                stealFullStack     = attributes["stealFullStack"].AsBool(stealFullStack);
                barFillSeconds = attributes["barFillSeconds"].AsFloat(barFillSeconds);
                cancelMaxDistance  = attributes["cancelMaxDistance"].AsFloat(cancelMaxDistance);
                mustStayBehind     = attributes["mustStayBehind"].AsBool(mustStayBehind);
                requireHandsEmpty  = attributes["requireHandsEmpty"].AsBool(requireHandsEmpty);
                inventoryOrder     = attributes["inventoryOrder"].AsArray<string>(inventoryOrder);
                slotBlacklist      = attributes["slotBlacklist"].AsArray<string>(slotBlacklist);
            }

            var cfg = ModConfig.Instance?.Pickpocket ?? new PickpocketingMainConfig();
            _featureEnabled = cfg.PickPocketing;
            baseSuccessChance = (float)GameMath.Clamp((float)cfg.baseSuccessChance, 0f, 1f);
            stealFullStack    = cfg.stealFullStack;
            _requiredTraits = cfg.RequiredTraits ?? _requiredTraits;
            barFillSeconds = Math.Max(0.1f, (float)cfg.PickpocketSeconds);
            if (sapi != null)
            {
                net = sapi.Network.GetChannel("thievery");
            }
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition,
            EnumInteractMode mode, ref EnumHandling handled)
        {
            if (!_featureEnabled) return;
            if (sapi == null) return;
            if (mode != EnumInteractMode.Interact) return;
            if (!(entity is EntityPlayer)) return;
            if (!(byEntity is EntityPlayer thiefAgent)) return;

            var thiefPlr  = thiefAgent.Player as IServerPlayer;
            var victimPlr = (entity as EntityPlayer)?.Player as IServerPlayer;
            if (thiefPlr == null || victimPlr == null) return;
            if (ReferenceEquals(thiefAgent, entity)) return;

            if (!HasAnyRequiredTrait(thiefPlr, _requiredTraits))
            {
                return;
            }
            double nowSec = sapi.World.Calendar.TotalHours * 3600;
            if (nowSec - lastAttemptTotalSeconds < attemptCooldownSec) return;
            if (!thiefAgent.Controls.Sneak)
            {
                return;
            }
            if (!thiefAgent.Controls.RightMouseDown)
            {
                return;
            }
            if (requireHandsEmpty && !HandsEmpty(thiefAgent))
            {
                return;
            }

            if (Distance2D(thiefAgent.ServerPos, entity.ServerPos) > attemptRange) return;
            if (mustStayBehind && !IsBehind(thiefAgent, entity as EntityAgent))
            {
                return;
            }

            if (sessions.ContainsKey(thiefPlr.PlayerUID)) return;

            var sess = new PickSession
            {
                Thief       = thiefPlr,
                ThiefAgent  = thiefAgent,
                StartMs     = sapi.World.ElapsedMilliseconds,
                DurationMs  = (long)Math.Max(300, barFillSeconds * 1000)
            };
            sessions[thiefPlr.PlayerUID] = sess;

            SendProgress(sess, 0f, true);

            if (tickListenerId == 0)
            {
                int everyMs = Math.Max(20, (int)(tickIntervalSec * 1000));
                tickListenerId = sapi.Event.RegisterGameTickListener(OnTick, everyMs);
            }

            handled = EnumHandling.PreventDefault;
        }

        void OnTick(float dt)
        {
            if (sapi == null || sessions.Count == 0)
            {
                StopTicking();
                return;
            }

            var remove = ListPool<string>.Rent();

            foreach (var kv in sessions)
            {
                var uid   = kv.Key;
                var sess  = kv.Value;
                if (sess.Closed) { remove.Add(uid); continue; }

                if (!sess.Thief?.ConnectionState.HasFlag(EnumClientState.Playing) ?? true)
                { Cancel(uid, notifyVictim: false); remove.Add(uid); continue; }

                var thief = sess.ThiefAgent;
                var victim = entity as EntityAgent;

                if (!thief.Controls.Sneak)
                { Cancel(uid); remove.Add(uid); continue; }
                
                if (!thief.Controls.RightMouseDown)
                { Cancel(uid); remove.Add(uid); continue; }

                if (requireHandsEmpty && !HandsEmpty(thief))
                { Cancel(uid); remove.Add(uid); continue; }

                if (Distance2D(thief.ServerPos, victim.ServerPos) > cancelMaxDistance)
                { Cancel(uid); remove.Add(uid); continue; }

                if (mustStayBehind && !IsBehind(thief, victim))
                { Cancel(uid); remove.Add(uid); continue; }

                long now = sapi.World.ElapsedMilliseconds;
                float p = GameMath.Clamp((now - sess.StartMs) / (float)sess.DurationMs, 0f, 1f);

                SendProgress(sess, p, true);

                if (p >= 1f)
                {
                    SendProgress(sess, 1f, true);

                    AttemptPickpocket(thief, victim);

                    lastAttemptTotalSeconds = sapi.World.Calendar.TotalHours * 3600;

                    sess.Closed = true;
                    remove.Add(uid);

                    var target = sess.Thief;
                    sapi.Event.RegisterCallback(dt =>
                    {
                        try
                        {
                            net?.SendPacket(new PickProgressPacket
                            {
                                Progress  = 1f,
                                IsPicking = false
                            }, target);
                        }
                        catch { }
                    }, Math.Max(50, (int)(tickIntervalSec * 1000)));
                }
            }

            foreach (var uid in remove) sessions.Remove(uid);
            ListPool<string>.Return(remove);

            if (sessions.Count == 0) StopTicking();
        }

        void AttemptPickpocket(EntityAgent thief, EntityAgent victim)
        {
            double finalChance = ComputeSuccessChance(thief, victim, autoTriggered: false);
            bool success = sapi.World.Rand.NextDouble() < finalChance;

            if (!success)
            {
                Feedback(thief, victim, success: false, moved: false, stack: null);
                return;
            }

            var moved = StealOne(thief, victim, out ItemStack movedStack);
            Feedback(thief, victim, success: true, moved: moved, stack: movedStack);
        }

        void Cancel(string uid, bool notifyVictim = true)
        {
            if (!sessions.TryGetValue(uid, out var sess)) return;
            SendProgress(sess, 0f, false);
            sess.Closed = true;

            if (notifyVictim && sapi.World.Rand.NextDouble() < ModConfig.Instance.Pickpocket.alertChance)
            {
                var v = (entity as EntityPlayer)?.Player as IServerPlayer;
            }
        }

        void StopTicking()
        {
            if (tickListenerId != 0)
            {
                sapi.Event.UnregisterGameTickListener(tickListenerId);
                tickListenerId = 0;
            }
        }

        void SendProgress(PickSession sess, float progress, bool isPicking)
        {
            try
            {
                net?.SendPacket(new PickProgressPacket
                {
                    Progress  = progress,
                    IsPicking = isPicking
                }, sess.Thief);
            }
            catch {}
        }

        static float Distance2D(EntityPos a, EntityPos b)
        {
            double dx = a.X - b.X;
            double dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        bool HandsEmpty(EntityAgent agent)
        {
            var right = agent.RightHandItemSlot;
            var left  = agent.LeftHandItemSlot;
            return (right == null || right.Itemstack == null) && (left == null || left.Itemstack == null);
        }

        bool IsBehind(EntityAgent thief, EntityAgent victim)
        {
            var toThief = (thief.ServerPos.XYZ - victim.ServerPos.XYZ).Normalize();
            double yaw = victim.ServerPos.Yaw;
            var victimForward = new Vec3d(Math.Sin(yaw), 0, Math.Cos(yaw));
            double dot = victimForward.X * toThief.X + victimForward.Z * toThief.Z;
            return dot < 0;
        }

        double ComputeSuccessChance(EntityAgent thief, EntityAgent victim, bool autoTriggered)
        {
            double c = baseSuccessChance;
            if (thief.Controls.Sneak) c += 0.10;
            if (autoTriggered) c -= 0.05;
            double horSpeed = Math.Sqrt(thief.ServerPos.Motion.X * thief.ServerPos.Motion.X +
                                        thief.ServerPos.Motion.Z * thief.ServerPos.Motion.Z);
            c -= GameMath.Clamp((float)horSpeed, 0f, 1f) * 0.10f;
            return GameMath.Clamp((float)c, 0.02f, 0.95f);
        }

        bool StealOne(EntityAgent thief, EntityAgent victim, out ItemStack movedStack)
        {
            movedStack = null;

            var thiefPlayer = (thief as EntityPlayer)?.Player as IServerPlayer;
            var victimPlayer = (victim as EntityPlayer)?.Player as IServerPlayer;
            if (thiefPlayer == null || victimPlayer == null) return false;

            var victimPim = victimPlayer.InventoryManager as PlayerInventoryManager;
            var thiefPim = thiefPlayer.InventoryManager as PlayerInventoryManager;
            if (victimPim == null || thiefPim == null) return false;

            var victimInvs = ResolveInventories(victimPim, inventoryOrder);
            var candidates = new List<(InventoryBase inv, ItemSlot slot)>();

            foreach (var inv in victimInvs)
            {
                if (!(inv?.InventoryID?.StartsWith("backpack-", StringComparison.OrdinalIgnoreCase) ?? false))
                    continue;

                foreach (var slot in inv)
                {
                    if (slot.Empty) continue;
                    if (!slot.CanTake()) continue;
                    if (IsBlacklistedSlot(inv, slot)) continue;

                    if (slot is ItemSlotBackpack) continue;

                    candidates.Add((inv, slot));
                }
            }

            if (candidates.Count == 0) return false;

            var pick = candidates[rnd.Next(candidates.Count)];
            var pickedSlot = pick.slot;

            int qty = stealFullStack ? pickedSlot.StackSize : 1;

            var taken = pickedSlot.TakeOut(qty);
            pickedSlot.MarkDirty();
            if (taken == null) return false;

            bool given = thiefPim.TryGiveItemstack(taken, slotNotifyEffect: true);
            if (!given) thief.World.SpawnItemEntity(taken, thief.ServerPos.XYZ);

            movedStack = taken;
            return true;
        }
        List<InventoryBase> ResolveInventories(PlayerInventoryManager pim, IEnumerable<string> ids)
        {
            var list = new List<InventoryBase>();
            foreach (var shortId in ids)
            {
                var invId = pim.GetInventoryName(shortId);
                if (pim.GetInventory(invId, out InventoryBase inv) && inv != null)
                {
                    list.Add(inv);
                }
            }
            return list;
        }

        bool IsBlacklistedSlot(InventoryBase inv, ItemSlot slot)
        {
            if (slotBlacklist == null || slotBlacklist.Length == 0) return false;

            string invId = inv?.InventoryID ?? string.Empty;
            int idx = inv.GetSlotId(slot);

            bool isHotbar()    => invId.StartsWith("hotbar-", StringComparison.OrdinalIgnoreCase);
            bool isCharacter() => invId.StartsWith("character-", StringComparison.OrdinalIgnoreCase);
            bool isBackpack()  => invId.StartsWith("backpack-", StringComparison.OrdinalIgnoreCase);

            foreach (var raw in slotBlacklist)
            {
                var key = raw?.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(key)) continue;

                if (key == "hotbar"    && isHotbar())    return true;
                if (key == "character" && isCharacter()) return true;
                if (key == "backpack"  && isBackpack())  return true;
                if (key == "armor"     && isCharacter()) return true;
                if (key == "offhand"   && isCharacter()) return true;

                int colon = key.IndexOf(':');
                if (colon > 0)
                {
                    var left  = key[..colon];
                    var right = key[(colon + 1)..];

                    if (left == "hotbar"    && isHotbar()    && MatchesIndex(right, idx)) return true;
                    if (left == "character" && isCharacter() && MatchesIndex(right, idx)) return true;
                    if (left == "backpack"  && isBackpack()  && MatchesIndex(right, idx)) return true;
                }
            }

            return false;

            static bool MatchesIndex(string spec, int idx)
            {
                if (idx < 0) return false;
                if (int.TryParse(spec, out int single)) return idx == single;

                var parts = spec.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int a) && int.TryParse(parts[1], out int b))
                {
                    if (a > b) (a, b) = (b, a);
                    return idx >= a && idx <= b;
                }

                return false;
            }
        }

        void Feedback(EntityAgent thief, EntityAgent victim, bool success, bool moved, ItemStack stack)
        {
            var t = (thief as EntityPlayer)?.Player as IServerPlayer;
            var v = (victim as EntityPlayer)?.Player as IServerPlayer;
            if (t == null || v == null) return;

            if (!success)
            {
                t.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("thievery:pickpocket-failed"), EnumChatType.Notification);
                if (sapi.World.Rand.NextDouble() < ModConfig.Instance.Pickpocket.alertChance)
                    v.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("thievery:brush-alert"), EnumChatType.Notification);
                return;
            }

            if (!moved || stack == null)
            {
                t.SendMessage(GlobalConstants.InfoLogChatGroup,
                    Lang.Get("thievery:stole-x-item",
                        stack.StackSize,
                        stack.Collectible?.GetHeldItemName(stack)),
                    EnumChatType.Notification);
                return;
            }

            t.SendMessage(GlobalConstants.InfoLogChatGroup,
                Lang.Get("thievery:stole-x-item", stack.StackSize, stack.Collectible?.GetHeldItemName(stack)),
                EnumChatType.Notification);
        }

        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            foreach (var kv in sessions)
            {
                var sess = kv.Value;
                if (!sess.Closed) SendProgress(sess, 0f, false);
                sess.Closed = true;
            }
            sessions.Clear();
            StopTicking();
            base.OnEntityDespawn(despawn);
        }
    }

    static class ListPool<T>
    {
        [ThreadStatic] static List<T> _l;
        public static List<T> Rent() => _l ??= new List<T>(4);
        public static void Return(List<T> l) { l.Clear(); }
    }
}
