using System;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Thievery.Config;
using Thievery.LockAndKey;
using Thievery.LockpickAndTensionWrench;
using Thievery.XSkill;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
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
        private XLibSkills xLibSkills;
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
            /*if (api.ModLoader.IsModEnabled("xlib") || api.ModLoader.IsModEnabled("xlibpatch"))
            {
                xLibSkills = new XLibSkills();
                xLibSkills.Initialize(api);
            }*/
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
                .SetMessageHandler<EndLockpickSessionPacket>(OnEndLockpickSession);
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
                        onRestoreEvent.AddEventHandler(carryEvents, new Action<BlockEntity, ITreeAttribute, bool>(OnCarryOnRestoreBlockEntity));
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
            if(api.ModLoader.IsModEnabled("configlib")) ConfigLibCompatibility.Init(api);
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
                .SetMessageHandler<PickProgressPacket>(OnPickProgressReceived)
                .SetMessageHandler<ItemDestroyedPacket>(OnItemDestroyedReceived);;
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
            api.World.Logger.Debug($"[Thievery] Server - Applied attributes to BlockEntityToolMold at {packet.BlockPos}: keyUID={packet.KeyUID}, keyName={packet.KeyName}");
        }
        private void OnLockPickCompletePacket(IServerPlayer player, LockPickCompletePacket packet)
        {
            var lockData = LockManager.GetLockData(packet.BlockPos);
            if (lockData == null) return;
            bool newState = lockData.IsLocked;
            switch (packet.Action)
            {
                case LockAction.Toggle:  newState = !lockData.IsLocked; break;
                case LockAction.Lock:    newState = true;               break;
                case LockAction.Unlock:  newState = false;              break;
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
            string existingToken = slot.Itemstack.Attributes.GetString(SessionAttrKey, null);

            int durability = slot.Itemstack.Attributes.GetInt("durability", 0);
            int maxDurability = slot.Itemstack.Collectible.Attributes?["durability"]?.AsInt(0) ?? 0;
            bool willDestroy = durability <= packet.Damage;

            slot.Itemstack.Collectible.DamageItem(player.Entity.World, player.Entity, slot, packet.Damage);

            if (willDestroy || slot.Itemstack == null)
            {
                serverChannel.SendPacket(new ItemDestroyedPacket { PlayerUid = player.PlayerUID }, player);
                return;
            }
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

            Stamp(p.ActiveInventoryId,  p.ActiveSlotId,  p.ActiveToken);
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

            Clear(p.ActiveInventoryId,  p.ActiveSlotId);
            Clear(p.OffhandInventoryId, p.OffhandSlotId);
        }
    }
}