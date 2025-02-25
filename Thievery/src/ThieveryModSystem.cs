using System;
using System.Collections.Generic;
using System.Linq;
using CarryOn;
using HarmonyLib;
using Newtonsoft.Json;
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
using Vintagestory.API.MathTools;
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
        public IClientNetworkChannel clientChannel;
        private IServerNetworkChannel serverChannel;
        public static Config.Config LoadedConfig { get; set; }
        private ConfigLibCompatibility _configLibCompatibility;
        private XLibSkills xLibSkills;
        private string configFilename = "ThieveryConfig.json";
        private ICoreServerAPI _serverApi;
        private ICoreClientAPI _clientApi;
        public override void StartPre(ICoreAPI _api)
        {
            /*if (api.ModLoader.IsModEnabled("xlib") || api.ModLoader.IsModEnabled("xlibpatch"))
            {
                xLibSkills = new XLibSkills();
                xLibSkills.Initialize(api);
            }*/
        }
        
        public override void Start(ICoreAPI Api)
        {
            this.api = Api;
            LoadConfig(api);
            base.Start(Api);
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
        private Config.Config LoadConfigFromFile(ICoreAPI api)
        {
            var jsonObj = api.LoadModConfig(configFilename);
            if (jsonObj == null)
            {
                return null;
            }
            var existingJson = JObject.Parse(jsonObj.Token.ToString());
            var configType = typeof(Config.Config);
            var properties = configType.GetProperties();
            var defaultConfig = new Config.Config();
            bool needsSave = false;
    
            foreach (var prop in properties)
            {
                string pascalCaseName = prop.Name;
                string camelCaseName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);

                // Check both pascal and camel case
                var hasValue = false;
                JToken value = null;
        
                if (existingJson.ContainsKey(pascalCaseName))
                {
                    value = existingJson[pascalCaseName];
                    hasValue = value != null && value.Type != JTokenType.Null;
                }
                else if (existingJson.ContainsKey(camelCaseName))
                {
                    value = existingJson[camelCaseName];
                    hasValue = value != null && value.Type != JTokenType.Null;
                }

                if (!hasValue)
                {
                    var defaultValue = prop.GetValue(defaultConfig);
                    existingJson[pascalCaseName] = JToken.FromObject(defaultValue);
                    needsSave = true;
                }
            }

            var settings = new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                DefaultValueHandling = DefaultValueHandling.Populate,
                NullValueHandling = NullValueHandling.Include
            };
    
            var config = JsonConvert.DeserializeObject<Config.Config>(existingJson.ToString(), settings);
    
            if (needsSave)
            {
                SaveConfig(api, config);
            }
    
            return config;
        }
        private void SaveConfig(ICoreAPI api, Config.Config config = null)
        {
            if (config == null)
            {
                config = LoadedConfig;
            }

            if (config == null)
            {
                return;
            }

            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Include,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver()
            };
            var configJson = JsonConvert.SerializeObject(config, jsonSettings);
            ModConfig.WriteConfig(api, configFilename, config);
        }
        private void LoadConfig(ICoreAPI api)
        {
            var savedConfig = LoadConfigFromFile(api);
            if (savedConfig == null)
            {
                LoadedConfig = new Config.Config();
                SaveConfig(api);
            }
            else
            {
                LoadedConfig = savedConfig;
            }
        }
        public override void StartServerSide(ICoreServerAPI Api)
        {
            base.StartServerSide(Api);
            LockManager = new LockManager(api);
            ICoreServerAPI sapi = api as ICoreServerAPI;
            _serverApi = Api;
            this.serverChannel = sapi.Network.RegisterChannel("thievery")
                .RegisterMessageType<PickProgressPacket>()
                .RegisterMessageType<TransformMoldPacket>()
                .RegisterMessageType<KeyNameUpdatePacket>()
                .RegisterMessageType<SyncKeyAttributesPacket>()
                .RegisterMessageType<ConfigSyncPacket>()
                .RegisterMessageType<ConfigSyncRequestPacket>()
                .RegisterMessageType<LockPickCompletePacket>()
                .SetMessageHandler<TransformMoldPacket>(OnTransformMoldRequest)
                .SetMessageHandler<SyncKeyAttributesPacket>(OnSyncKeyAttributesPacket)
                .SetMessageHandler<ConfigSyncRequestPacket>(OnConfigSyncRequestReceived)
                .SetMessageHandler<KeyNameUpdatePacket>((player, packet) =>
                {
                    var slot = player.InventoryManager.ActiveHotbarSlot;
                    if (slot?.Itemstack == null) return;
                    slot.Itemstack.Attributes.SetString("keyName", packet.KeyName);
                    slot.MarkDirty();
                })
                .SetMessageHandler<LockPickCompletePacket>(OnLockPickCompletePacket);
            if (api.ModLoader.IsModEnabled("carryon"))
            {
                var carrySystem = sapi.ModLoader.GetModSystem<CarrySystem>();
                carrySystem.CarryEvents.OnRestoreEntityBlockData += OnCarryOnRestoreBlockEntity;
            }
        }

        public override void StartClientSide(ICoreClientAPI Api)
        {
            base.StartClientSide(Api);
            LockManager = new LockManager(Api);
            lockpickHudElement = new LockpickHudElement(Api);
            _clientApi = Api;
            Api.Event.RegisterRenderer(lockpickHudElement, EnumRenderStage.Ortho, "lockpickhud");
            _configLibCompatibility = new ConfigLibCompatibility((ICoreClientAPI)api);
            clientChannel = Api.Network.RegisterChannel("thievery")
                .RegisterMessageType<PickProgressPacket>()
                .RegisterMessageType<TransformMoldPacket>()
                .RegisterMessageType<KeyNameUpdatePacket>()
                .RegisterMessageType<SyncKeyAttributesPacket>()
                .RegisterMessageType<ConfigSyncPacket>()
                .RegisterMessageType<ConfigSyncRequestPacket>()
                .RegisterMessageType<LockPickCompletePacket>()
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
            var configToSend = new Config.Config(_serverApi, LoadedConfig);
            var configSyncPacket = new ConfigSyncPacket
            {
                ServerConfig = configToSend,
            };
            serverChannel.SendPacket(configSyncPacket, fromPlayer);
        }

        private void OnConfigSyncReceived(ConfigSyncPacket packet)
        {
            if (_clientApi == null) return;
            LoadedConfig = packet.ServerConfig;
        }
        private void OnLockPickCompletePacket(IServerPlayer player, LockPickCompletePacket packet)
        {
            var lockData = LockManager.GetLockData(packet.BlockPos);
            if (lockData != null && lockData.IsLocked)
            {
                LockManager.SetLock(packet.BlockPos, lockData.LockUid, false);
                _serverApi.World.PlaySoundAt(
                    new AssetLocation("thievery:sounds/lock"),
                    packet.BlockPos,
                    0,
                    null,
                    true,
                    32f,
                    1f
                );
            }
        }
        private void OnCarryOnRestoreBlockEntity(BlockEntity blockEntity, ITreeAttribute blockEntityData, bool dropped)
        {
            var lockBehavior = blockEntity?.GetBehavior<BlockEntityThieveryLockData>();
            if (lockBehavior != null)
            {
                lockBehavior.FromTreeAttributes(blockEntityData, blockEntity.Api.World);
            }
        }
    }
}