using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using ProtoBuf;
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
using XLib.XLeveling;

namespace Thievery
{
    public class ThieveryModSystem : ModSystem
    {
        private ICoreAPI api;
        public LockManager LockManager { get; private set; }
        private Harmony harmony;
        private LockpickHudElement lockpickHudElement;
        public LockpickHudElement LockpickHudElement => lockpickHudElement;
        private IClientNetworkChannel clientChannel;
        private IServerNetworkChannel serverChannel;
        public static Config.Config LoadedConfig { get; set; }
        private ConfigLibCompatibility _configLibCompatibility;
        private XLibSkills xLibSkills;
        public override void StartPre(ICoreAPI api)
        {
            if (api.ModLoader.IsModEnabled("xlib") || api.ModLoader.IsModEnabled("xlibpatch"))
            {
                xLibSkills = new XLibSkills();
                xLibSkills.Initialize(api);
            }
        }
        
        public override void Start(ICoreAPI Api)
        {
            this.api = Api;
            LoadConfig(api);
            base.Start(Api);
            LoadedConfig = ModConfig.ReadConfig<Config.Config>(api, "ThieveryConfig.json");
            harmony = new Harmony("com.thieverymod");
            harmony.PatchAll();
            Api.RegisterItemClass("ItemKey", typeof(ItemKey));
            Api.RegisterItemClass("ItemLockpick", typeof(ItemLockpick));
            Api.RegisterItemClass("ItemTensionWrench", typeof(ItemTensionWrench));
            Api.RegisterItemClass("ItemLockTool", typeof(ItemLockTool));
            Api.RegisterBlockClass("BlockKeyMold", typeof(BlockKeyMold));
            Api.RegisterBlockEntityClass("BlockEntityKeyMold", typeof(BlockEntityKeyMold));
            Api.RegisterBlockEntityBehaviorClass("ThieveryLockData", typeof(BlockEntityThieveryLockData));
        }
        private void LoadConfig(ICoreAPI api)
        {
            LoadedConfig = ModConfig.ReadConfig<Config.Config>(api, "ThieveryConfig.json");
            if (LoadedConfig == null)
            {
                LoadedConfig = new Config.Config();
                ModConfig.WriteConfig(api, "ThieveryConfig.json", LoadedConfig);
            }
        }
        public override void StartServerSide(ICoreServerAPI Api)
        {
            base.StartServerSide(Api);
            LockManager = new LockManager(api);
            ICoreServerAPI sapi = api as ICoreServerAPI;
            this.serverChannel = sapi.Network.RegisterChannel("thievery")
                .RegisterMessageType<PickProgressPacket>()
                .RegisterMessageType<TransformMoldPacket>()
                .RegisterMessageType<KeyNameUpdatePacket>()
                .RegisterMessageType<SyncKeyAttributesPacket>()
                .RegisterMessageType<ConfigSyncPacket>()
                .RegisterMessageType<ConfigSyncRequestPacket>()
                .SetMessageHandler<TransformMoldPacket>(OnTransformMoldRequest)
                .SetMessageHandler<SyncKeyAttributesPacket>(OnSyncKeyAttributesPacket)
                .SetMessageHandler<ConfigSyncRequestPacket>(OnConfigSyncRequestReceived)
                .SetMessageHandler<KeyNameUpdatePacket>((player, packet) =>
                {
                    var slot = player.InventoryManager.ActiveHotbarSlot;
                    if (slot?.Itemstack == null) return;
                    slot.Itemstack.Attributes.SetString("keyName", packet.KeyName);
                    slot.MarkDirty();
                });
        }

        public override void StartClientSide(ICoreClientAPI Api)
        {
            base.StartClientSide(Api);
            LockManager = new LockManager(Api);
            lockpickHudElement = new LockpickHudElement(Api);
            Api.Event.RegisterRenderer(lockpickHudElement, EnumRenderStage.Ortho, "lockpickhud");
            _configLibCompatibility = new ConfigLibCompatibility((ICoreClientAPI)api);
            clientChannel = Api.Network.RegisterChannel("thievery")
                .RegisterMessageType<PickProgressPacket>()
                .RegisterMessageType<TransformMoldPacket>()
                .RegisterMessageType<KeyNameUpdatePacket>()
                .RegisterMessageType<SyncKeyAttributesPacket>()
                .RegisterMessageType<ConfigSyncPacket>()
                .SetMessageHandler<PickProgressPacket>(OnPickProgressReceived)
                .SetMessageHandler<ConfigSyncPacket>(OnConfigSyncReceived);
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
                    if (block.EntityClass == null)
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
            harmony.UnpatchAll("com.thieverymod");
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
        [ProtoContract]
        public class ConfigSyncPacket
        {
            [ProtoMember(1)] public Config.Config ServerConfig { get; set; }
        }
        [ProtoContract]
        public class ConfigSyncRequestPacket
        {
        }

        private void OnConfigSyncRequestReceived(IServerPlayer fromPlayer, ConfigSyncRequestPacket packet)
        {
            var configSyncPacket = new ConfigSyncPacket
            {
                ServerConfig = LoadedConfig,
            };

            serverChannel.SendPacket(configSyncPacket, fromPlayer);
        }
        private void OnConfigSyncReceived(ConfigSyncPacket packet)
        {
            LoadedConfig = new Config.Config
            {
                LockPicking = packet.ServerConfig.LockPicking,
                BlackBronzePadlockPickDurationSeconds = packet.ServerConfig.BlackBronzePadlockPickDurationSeconds,
                BismuthBronzePadlockPickDurationSeconds = packet.ServerConfig.BismuthBronzePadlockPickDurationSeconds,
                TinBronzePadlockPickDurationSeconds = packet.ServerConfig.TinBronzePadlockPickDurationSeconds,
                IronPadlockPickDurationSeconds = packet.ServerConfig.IronPadlockPickDurationSeconds,
                MeteoricIronPadlockPickDurationSeconds = packet.ServerConfig.MeteoricIronPadlockPickDurationSeconds,
                SteelPadlockPickDurationSeconds = packet.ServerConfig.SteelPadlockPickDurationSeconds,
                LockPickDamageChance = packet.ServerConfig.LockPickDamageChance,
                LockPickDamage = packet.ServerConfig.LockPickDamage,
                RequiresPilferer = packet.ServerConfig.RequiresPilferer,
                RequiresTinkerer = packet.ServerConfig.RequiresTinkerer
            };
        }
    }
}