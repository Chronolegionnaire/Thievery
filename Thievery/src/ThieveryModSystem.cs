using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Thievery.Config;
using Thievery.LockAndKey;
using Thievery.LockpickAndTensionWrench;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Thievery
{
    public class ThieveryModSystem : ModSystem
    {
        private ICoreAPI api;
        public LockManager LockManager { get; private set; }
        private Harmony harmony;
        private LockpickHudElement lockpickHudElement;
        public LockpickHudElement LockpickHudElement => lockpickHudElement;
        public IClientNetworkChannel clientChannel;
        private IServerNetworkChannel serverChannel;
        private ConfigLibCompatibility _configLibCompatibility;
        private ICoreServerAPI _serverApi;
        private ICoreClientAPI _clientApi;
        private const string SessionAttrKey = "thievery.lockpickSessionToken";
        public const string HarmonyID = "com.chronolegionnaire.thievery";
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            ConfigManager.EnsureModConfigLoaded(api);
            if (!Harmony.HasAnyPatches(HarmonyID))
            {
                harmony = new Harmony(HarmonyID);
                harmony.PatchAll();
            }
        }

        public override void Start(ICoreAPI Api)
        {
            this.api = Api;
            base.Start(Api);
            Api.RegisterItemClass("ItemKey", typeof(ItemKey));
            Api.RegisterItemClass("ItemLockpick", typeof(ItemLockpick));
            Api.RegisterItemClass("ItemTensionWrench", typeof(ItemTensionWrench));
            Api.RegisterItemClass("ItemLockTool", typeof(ItemLockTool));
            Api.RegisterBlockClass("BlockKeyMold", typeof(BlockKeyMold));
            Api.RegisterBlockEntityClass("BlockEntityKeyMold", typeof(BlockEntityKeyMold));
            Api.RegisterBlockEntityBehaviorClass("ThieveryLockData", typeof(BlockEntityThieveryLockData));
        }
        public override void StartServerSide(ICoreServerAPI Api)
        {
            base.StartServerSide(Api);
            LockManager = new LockManager(api);
            ICoreServerAPI sapi = api as ICoreServerAPI;
            _serverApi = Api;
            this.serverChannel = Api.Network.RegisterChannel("thievery")
                .RegisterMessageType<PickProgressPacket>()
                .RegisterMessageType<TransformMoldPacket>()
                .RegisterMessageType<KeyNameUpdatePacket>()
                .RegisterMessageType<SyncKeyAttributesPacket>()
                .RegisterMessageType<LockPickCompletePacket>()
                .RegisterMessageType<ItemDamagePacket>()
                .RegisterMessageType<ItemDestroyedPacket>()
                .RegisterMessageType<StartLockpickSessionPacket>()
                .RegisterMessageType<EndLockpickSessionPacket>()
                .RegisterMessageType<LockPickLockoutPacket>()
                .RegisterMessageType<WorldgenPickRewardPacket>()
                .SetMessageHandler<TransformMoldPacket>(OnTransformMoldRequest)
                .SetMessageHandler<SyncKeyAttributesPacket>(OnSyncKeyAttributesPacket)
                .SetMessageHandler<KeyNameUpdatePacket>((player, packet) =>
                {
                    var slot = player.InventoryManager.ActiveHotbarSlot;
                    if (slot?.Itemstack == null) return;
                    slot.Itemstack.Attributes.SetString("keyName", packet.KeyName);
                    slot.MarkDirty();
                })
                .SetMessageHandler<LockPickCompletePacket>(OnLockPickCompletePacket)
                .SetMessageHandler<ItemDamagePacket>(OnItemDamagePacketReceived)
                .SetMessageHandler<StartLockpickSessionPacket>(OnStartLockpickSession)
                .SetMessageHandler<EndLockpickSessionPacket>(OnEndLockpickSession)
                .SetMessageHandler<LockPickLockoutPacket>(OnLockPickLockoutPacket)
                .SetMessageHandler<WorldgenPickRewardPacket>(OnWorldgenPickRewardPacket);
            if (api.ModLoader.IsModEnabled("carryon"))
            {
                var carrySystem = Api.ModLoader.GetModSystem("CarrySystem");
                if (carrySystem != null)
                {
                    var carrySystemType = carrySystem.GetType();
                    var eventsProperty = carrySystemType.GetProperty("CarryEvents");
                    var carryEvents = eventsProperty?.GetValue(carrySystem);
                    var onRestoreEvent = carryEvents?.GetType().GetEvent("OnRestoreEntityBlockData");
                    if (onRestoreEvent != null)
                    {
                        onRestoreEvent.AddEventHandler(carryEvents,
                            new Action<BlockEntity, ITreeAttribute, bool>(OnCarryOnRestoreBlockEntity));
                    }
                }
            }
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            LockManager = new LockManager(api);
            lockpickHudElement = new LockpickHudElement(api);
            _clientApi = api;
            if (api.ModLoader.IsModEnabled("configlib")) ConfigLibCompatibility.Init(api);
            api.Event.RegisterRenderer(lockpickHudElement, EnumRenderStage.Ortho, "lockpickhud");
            clientChannel = api.Network.RegisterChannel("thievery")
                .RegisterMessageType<PickProgressPacket>()
                .RegisterMessageType<TransformMoldPacket>()
                .RegisterMessageType<KeyNameUpdatePacket>()
                .RegisterMessageType<SyncKeyAttributesPacket>()
                .RegisterMessageType<LockPickCompletePacket>()
                .RegisterMessageType<ItemDamagePacket>()
                .RegisterMessageType<ItemDestroyedPacket>()
                .RegisterMessageType<StartLockpickSessionPacket>()
                .RegisterMessageType<EndLockpickSessionPacket>()
                .RegisterMessageType<LockPickLockoutPacket>()
                .RegisterMessageType<WorldgenPickRewardPacket>()
                .SetMessageHandler<PickProgressPacket>(OnPickProgressReceived)
                .SetMessageHandler<ItemDestroyedPacket>(OnItemDestroyedReceived);
        }

        public override void AssetsFinalize(ICoreAPI Api)
        {
            base.AssetsFinalize(Api);
            foreach (var collectible in Api.World.Collectibles)
            {
                if (collectible is Block block &&
                    block.CollectibleBehaviors != null &&
                    block.CollectibleBehaviors.Any(b => b.GetType().Name == "BlockBehaviorLockable"))
                {
                    if (string.IsNullOrEmpty(block.EntityClass))
                    {
                        block.EntityClass = "Generic";
                    }

                    if (block.BlockEntityBehaviors == null)
                    {
                        block.BlockEntityBehaviors = new BlockEntityBehaviorType[0];
                    }

                    bool hasBehavior = block.BlockEntityBehaviors.Any(b => b.Name == "ThieveryLockData");
                    if (!hasBehavior)
                    {
                        var newBEBehavior = new BlockEntityBehaviorType()
                        {
                            Name = "ThieveryLockData",
                            properties = new JsonObject(new JObject())
                        };

                        var beList = block.BlockEntityBehaviors.ToList();
                        beList.Add(newBEBehavior);
                        block.BlockEntityBehaviors = beList.ToArray();
                    }
                }
            }
        }

        private void OnPickProgressReceived(PickProgressPacket packet)
        {
            if (lockpickHudElement == null) return;

            lockpickHudElement.CircleProgress = packet.Progress;
            lockpickHudElement.CircleVisible = packet.IsPicking;
        }

        public override void Dispose()
        {
            harmony.UnpatchAll(HarmonyID);
            ConfigManager.UnloadModConfig();
            base.Dispose();
        }

        private void OnTransformMoldRequest(IServerPlayer player, TransformMoldPacket packet)
        {
            var api = player.Entity.World.Api;
            var groundStorageEntity =
                api.World.BlockAccessor.GetBlockEntity(packet.BlockPos) as BlockEntityGroundStorage;

            if (groundStorageEntity == null)
            {
                return;
            }

            if (packet.SlotIndex < 0 || packet.SlotIndex >= groundStorageEntity.Inventory.Count)
            {
                return;
            }

            var inventorySlot = groundStorageEntity.Inventory[packet.SlotIndex];
            if (inventorySlot?.Itemstack?.Block?.Code?.Path.Contains("keymoldpre") == true)
            {
                var newBlock = api.World.GetBlock(new AssetLocation("thievery:keymold-raw-key-north"));
                if (newBlock == null)
                {
                    return;
                }

                var newItemStack = new ItemStack(newBlock);
                if (!string.IsNullOrEmpty(packet.KeyUID))
                {
                    newItemStack.Attributes.SetString("keyUID", packet.KeyUID);
                }

                if (!string.IsNullOrEmpty(packet.KeyName))
                {
                    newItemStack.Attributes.SetString("keyName", packet.KeyName);
                }
                else
                {
                    newItemStack.Attributes.SetString("keyName", Lang.Get("thievery:key"));
                }

                inventorySlot.Itemstack = newItemStack;
                groundStorageEntity.Inventory[packet.SlotIndex].MarkDirty();
                groundStorageEntity.MarkDirty(true);
                api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/clayform"), packet.BlockPos.X,
                    packet.BlockPos.Y, packet.BlockPos.Z, null, true, 16);
            }
        }

        private void OnSyncKeyAttributesPacket(IServerPlayer player, SyncKeyAttributesPacket packet)
        {
            var api = player.Entity.World.Api;
            var blockEntity = api.World.BlockAccessor.GetBlockEntity(packet.BlockPos);

            if (blockEntity == null)
            {
                api.World.Logger.Warning($"[Thievery] BlockEntityToolMold not found at {packet.BlockPos}.");
                return;
            }

            var tree = new TreeAttribute();
            tree.SetString("keyUID", packet.KeyUID);
            tree.SetString("keyName", packet.KeyName);
            blockEntity.FromTreeAttributes(tree, api.World);
            blockEntity.MarkDirty(true);
            api.World.Logger.Debug(
                $"[Thievery] Server - Applied attributes to BlockEntityToolMold at {packet.BlockPos}: keyUID={packet.KeyUID}, keyName={packet.KeyName}");
        }

        private void OnLockPickCompletePacket(IServerPlayer player, LockPickCompletePacket packet)
        {
            var lockData = LockManager.GetLockData(packet.BlockPos);
            if (lockData == null) return;
            bool newState = lockData.IsLocked;
            switch (packet.Action)
            {
                case LockAction.Toggle: newState = !lockData.IsLocked; break;
                case LockAction.Lock: newState = true; break;
                case LockAction.Unlock: newState = false; break;
            }

            LockManager.SetLock(packet.BlockPos, lockData.LockUid, newState);
            _serverApi.World.PlaySoundAt(
                new AssetLocation("thievery:sounds/lock"),
                packet.BlockPos, 0, null, true, 32f, 1f
            );
        }

        private void OnCarryOnRestoreBlockEntity(BlockEntity blockEntity, ITreeAttribute blockEntityData, bool dropped)
        {
            var lockBehavior = blockEntity?.GetBehavior<BlockEntityThieveryLockData>();
            if (lockBehavior != null)
            {
                lockBehavior.FromTreeAttributes(blockEntityData, blockEntity.Api.World);
            }
        }

        private void OnItemDamagePacketReceived(IServerPlayer player, ItemDamagePacket packet)
        {
            var inventory = player.InventoryManager.GetInventory(packet.InventoryId);
            if (inventory == null) return;
            if (packet.SlotId < 0 || packet.SlotId >= inventory.Count) return;

            var slot = inventory[packet.SlotId];
            if (slot?.Itemstack == null) return;

            int damage = packet.Damage;
            int durability = slot.Itemstack.Attributes.GetInt("durability", 0);
            bool willDestroy = durability <= damage;

            slot.Itemstack.Collectible.DamageItem(player.Entity.World, player.Entity, slot, damage);

            if (willDestroy || slot.Itemstack == null)
            {
                serverChannel.SendPacket(new ItemDestroyedPacket { PlayerUid = player.PlayerUID }, player);
                return;
            }

            string existingToken = slot.Itemstack.Attributes.GetString(SessionAttrKey, null);
            if (!string.IsNullOrEmpty(existingToken))
            {
                slot.Itemstack.Attributes.SetString(SessionAttrKey, existingToken);
                slot.MarkDirty();
            }
        }


        private void OnItemDestroyedReceived(ItemDestroyedPacket packet)
        {
            if (_clientApi == null) return;
            var player = _clientApi.World.Player;
            if (player == null || player.PlayerUID != packet.PlayerUid) return;
            var lockpickItems = _clientApi.World.Items
                .Where(item => item is ItemLockpick)
                .Cast<ItemLockpick>();

            foreach (var lockpickItem in lockpickItems)
            {
                lockpickItem.StopPickingForPlayer(player);
            }

            if (LockpickHudElement != null)
            {
                LockpickHudElement.CircleVisible = false;
                LockpickHudElement.CircleProgress = 0f;
            }
        }

        private void OnStartLockpickSession(IServerPlayer player, StartLockpickSessionPacket p)
        {
            void Stamp(string invId, int slotId, string token)
            {
                var inv = player.InventoryManager.GetInventory(invId);
                var slot = inv?.ElementAtOrDefault(slotId);
                if (slot?.Itemstack == null || string.IsNullOrEmpty(token)) return;
                var code = slot.Itemstack.Collectible?.Code?.Path ?? "";
                if (!(code.StartsWith("lockpick-") || code.StartsWith("tensionwrench-"))) return;

                slot.Itemstack.Attributes.SetString(SessionAttrKey, token);
                slot.MarkDirty();
            }

            Stamp(p.ActiveInventoryId, p.ActiveSlotId, p.ActiveToken);
            Stamp(p.OffhandInventoryId, p.OffhandSlotId, p.OffhandToken);
        }

        private void OnEndLockpickSession(IServerPlayer player, EndLockpickSessionPacket p)
        {
            void Clear(string invId, int slotId)
            {
                var inv = player.InventoryManager.GetInventory(invId);
                var slot = inv?.ElementAtOrDefault(slotId);
                if (slot?.Itemstack == null) return;

                slot.Itemstack.Attributes.RemoveAttribute(SessionAttrKey);
                slot.MarkDirty();
            }

            Clear(p.ActiveInventoryId, p.ActiveSlotId);
            Clear(p.OffhandInventoryId, p.OffhandSlotId);
        }

        private void OnLockPickLockoutPacket(IServerPlayer fromPlayer, LockPickLockoutPacket pkt)
        {
            try
            {
                var world = api.World;
                if (world == null || pkt?.BlockPos == null) return;

                var be = world.BlockAccessor.GetBlockEntity(pkt.BlockPos);
                if (be == null) return;

                var beh = be.GetBehavior<BlockEntityThieveryLockData>();
                if (beh == null) return;

                if (!string.IsNullOrEmpty(beh.LockUID) &&
                    !string.IsNullOrEmpty(pkt.LockUid) &&
                    !beh.LockUID.Equals(pkt.LockUid))
                {
                    return;
                }

                bool permanentCfg = ModConfig.Instance?.MiniGame?.PermanentLockBreak == true;

                bool isWorldgen = WorldgenLockUtils.IsWorldgenLockAt(world, pkt.BlockPos, beh.LockUID);

                long until;
                if (ModConfig.Instance?.MiniGame?.PermanentLockBreak == true || pkt.LockoutUntilMs == -1)
                {
                    until = -1;
                }
                else
                {
                    long now = world.ElapsedMilliseconds;
                    long mins = Math.Max(1, ModConfig.Instance?.MiniGame?.ProbeLockoutMinutes ?? 10);
                    until = now + mins * 60L * 1000L;
                }

                if (isWorldgen)
                {
                    beh.SetGlobalLockout(until);
                }
                else
                {
                    beh.SetPerPlayerLockout(fromPlayer.PlayerUID, until);
                }

                be.MarkDirty(true);

                var sapi = api as ICoreServerAPI;
                sapi?.Network?.BroadcastBlockEntityPacket(pkt.BlockPos, ThieveryPacketIds.LockStateSync, new LockData
                {
                    LockUid = beh.LockUID,
                    IsLocked = beh.LockedState,
                    LockType = beh.LockType,
                    LockoutUntilMs = until
                });
            }
            catch
            {
            }
        }
        private void OnWorldgenPickRewardPacket(IServerPlayer fromPlayer, WorldgenPickRewardPacket pkt)
        {
            var sapi = api as ICoreServerAPI;
            if (sapi == null || pkt?.BlockPos == null) return;

            sapi.Event.RegisterCallback((dt) =>
            {
                var be = sapi.World.BlockAccessor.GetBlockEntity(pkt.BlockPos);
                if (be == null) return;

                var beh = be.GetBehavior<BlockEntityThieveryLockData>();
                if (beh == null) return;

                if (!string.IsNullOrEmpty(beh.LockUID) &&
                    !string.IsNullOrEmpty(pkt.LockUid) &&
                    beh.LockUID.Equals(pkt.LockUid) &&
                    beh.LockedState == false)
                {
                    WorldgenPickRewardGiver.GrantWorldgenPickRewardOnce(sapi, pkt.BlockPos, pkt.LockType, pkt.LockUid, fromPlayer);
                }
            }, 0);
        }

        public static class WorldgenPickRewardGiver
        {
            public static void GrantWorldgenPickRewardOnce(ICoreServerAPI sapi, BlockPos pos, string lockType, string lockUid, IServerPlayer player)
            {
                var be = sapi.World.BlockAccessor.GetBlockEntity(pos);
                if (be == null) return;

                var beLock = be.GetBehavior<BlockEntityThieveryLockData>();
                if (beLock == null) return;

                if (beLock.WorldGenPickRewardGranted) return;

                if (!WorldgenLockUtils.IsWorldgenLockAt(sapi.World, pos, beLock.LockUID))
                    return;

                int difficulty = DifficultyHelper.GetLockDifficulty(sapi, lockType);

                if (be is BlockEntityGenericTypedContainer container)
                {
                    GiveContainerLoot(sapi, container, difficulty, player);
                }
                else
                {
                    DropRustyGears(sapi, pos, difficulty);
                }

                beLock.WorldGenPickRewardGranted = true;
                be.MarkDirty(true);
            }

            private static void GiveContainerLoot(ICoreServerAPI sapi, BlockEntityGenericTypedContainer container, int difficulty, IServerPlayer player)
            {
                var rng   = sapi.World.Rand ?? Random.Shared;
                var items = WorldgenPickRewards.RollLoot(sapi, difficulty, rng, player);
                var inv   = container.Inventory;

                foreach (var st in items)
                {
                    bool placed = false;

                    for (int i = 0; i < inv.Count; i++)
                    {
                        var slot = inv[i];
                        if (!slot.Empty) continue;

                        slot.Itemstack = st;

                        if (slot.Itemstack?.Collectible is IResolvableCollectible rc)
                        {
                            rc.Resolve(slot, sapi.World, true);
                        }

                        slot.MarkDirty();
                        placed = true;
                        break;
                    }

                    if (!placed)
                    {
                        var toDrop = st;
                        if (toDrop?.Collectible is IResolvableCollectible rc)
                        {
                            var dummyInv  = new DummyInventory(sapi, 1);
                            var dummySlot = new ItemSlot(dummyInv) { Itemstack = toDrop };
                            rc.Resolve(dummySlot, sapi.World, true);
                            toDrop = dummySlot.Itemstack;
                        }

                        if (toDrop != null)
                        {
                            var dropPos = container.Pos.ToVec3d().Add(0.5, 0.75, 0.5);
                            sapi.World.SpawnItemEntity(toDrop, dropPos);
                        }
                    }
                }

                container.MarkDirty(true);
            }

            private static void DropRustyGears(ICoreServerAPI sapi, BlockPos pos, int difficulty)
            {
                var gear = sapi.World.GetItem(new AssetLocation("game:gear-rusty"));
                if (gear == null) return;

                var rng = sapi.World.Rand ?? Random.Shared;
                int amount = WorldgenPickRewards.RustyGearsForDifficulty(difficulty, rng);

                var stack = new ItemStack(gear, amount);
                var dropPos = pos.ToVec3d().Add(0.5, 0.75, 0.5);
                sapi.World.SpawnItemEntity(stack, dropPos);
            }
        }
        internal static class DifficultyHelper
        {
            public static int GetLockDifficulty(ICoreAPI api, string lockType)
            {
                var diff = Thievery.Config.ModConfig.Instance?.Difficulty;

                var padlockDifficulties = new Dictionary<string, int>
                {
                    { "padlock-blackbronze",   diff?.BlackBronzePadlockDifficulty ?? 25 },
                    { "padlock-bismuthbronze", diff?.BismuthBronzePadlockDifficulty ?? 30 },
                    { "padlock-tinbronze",     diff?.TinBronzePadlockDifficulty ?? 35 },
                    { "padlock-iron",          diff?.IronPadlockDifficulty ?? 60 },
                    { "padlock-meteoriciron",  diff?.MeteoricIronPadlockDifficulty ?? 70 },
                    { "padlock-steel",         diff?.SteelPadlockDifficulty ?? 80 },
                    { "padlock-copper",        diff?.CopperPadlockDifficulty ?? 10 },
                    { "padlock-nickel",        diff?.NickelPadlockDifficulty ?? 15 },
                    { "padlock-silver",        diff?.SilverPadlockDifficulty ?? 35 },
                    { "padlock-gold",          diff?.GoldPadlockDifficulty ?? 20 },
                    { "padlock-titanium",      diff?.TitaniumPadlockDifficulty ?? 90 },
                    { "padlock-lead",          diff?.LeadPadlockDifficulty ?? 20 },
                    { "padlock-zinc",          diff?.ZincPadlockDifficulty ?? 25 },
                    { "padlock-tin",           diff?.TinPadlockDifficulty ?? 20 },
                    { "padlock-chromium",      diff?.ChromiumPadlockDifficulty ?? 60 },
                    { "padlock-cupronickel",   diff?.CupronickelPadlockDifficulty ?? 40 },
                    { "padlock-electrum",      diff?.ElectrumPadlockDifficulty ?? 20 },
                    { "padlock-platinum",      diff?.PlatinumPadlockDifficulty ?? 50 },
                };

                if (padlockDifficulties.TryGetValue(lockType, out int d))
                    return GameMath.Clamp(d, 1, 100);

                return 50;
            }
        }
    }
}