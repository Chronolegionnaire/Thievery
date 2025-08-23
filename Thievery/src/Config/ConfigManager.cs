using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Thievery.Config.SubConfigs;

namespace Thievery.Config;

public static class ConfigManager
{
    // Ensures collections get replaced (not merged/appended) when populating existing instances.
    private static readonly JsonSerializerSettings ReplaceSettings = new()
    {
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };


    public static void EnsureModConfigLoaded(ICoreAPI api)
    {
        if (ModConfig.Instance is not null) return; // Config already loaded

        if (api.Side == EnumAppSide.Server)
        {
            LoadModConfigFromDisk(api);
            StoreModConfigToWorldConfig(api);
        }
        else
        {
            LoadModConfigFromWorldConfig(api);
        }
    }

    private static void LoadModConfigFromDisk(ICoreAPI api)
    {
        try
        {
            // If the file exists, this returns the deserialized config (VS API typically creates a new instance).
            // If not found, it returns null.
            ModConfig.Instance = api.LoadModConfig<ModConfig>(ModConfig.ConfigPath);

            // Migrate any legacy fields first (works whether Instance is null or not).
            if (ModConfig.Instance is not null)
            {
                MapLegacyData(ModConfig.Instance);
            }

            // Create brand-new config if nothing on disk
            ModConfig.Instance ??= new ModConfig();

            // Ensure subconfig defaults are seeded ONLY when missing.
            EnsureSubconfigDefaults(ModConfig.Instance);

            // Save back (also normalizes on-disk JSON after legacy mapping/default hydration).
            SaveModConfig(api, ModConfig.Instance);
        }
        catch (Exception ex)
        {
            api.Logger.Error(ex);
            api.Logger.Warning("[{0}] using default config", "Thievery");
            ModConfig.Instance = new ModConfig();
            EnsureSubconfigDefaults(ModConfig.Instance);
        }
    }

    public static void StoreModConfigToWorldConfig(ICoreAPI api)
    {
        var serializedConfig = JsonConvert.SerializeObject(ModConfig.Instance, Formatting.None);

        // Base64 encode for safety (base game StringAttribute does not escape correctly when converting to JToken)
        var base64EncodedConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes(serializedConfig));
        api.World.Config.SetString(ModConfig.ConfigPath, base64EncodedConfig);
    }

    private static void LoadModConfigFromWorldConfig(ICoreAPI api)
    {
        var base64EncodedConfig = api.World.Config.GetString(ModConfig.ConfigPath);

        // If nothing stored yet, fall back to defaults
        if (string.IsNullOrWhiteSpace(base64EncodedConfig))
        {
            ModConfig.Instance = new ModConfig();
            EnsureSubconfigDefaults(ModConfig.Instance);
            return;
        }

        try
        {
            var serializedConfig = Encoding.UTF8.GetString(Convert.FromBase64String(base64EncodedConfig));

            // Populate into a fresh instance using Replace to avoid collection merge/appends.
            var cfg = new ModConfig();
            JsonConvert.PopulateObject(serializedConfig, cfg, ReplaceSettings);
            ModConfig.Instance = cfg;

            MapLegacyData(ModConfig.Instance);
            EnsureSubconfigDefaults(ModConfig.Instance);
        }
        catch (Exception)
        {
            // If anything goes wrong with world-config decode/parse, just use defaults.
            ModConfig.Instance = new ModConfig();
            EnsureSubconfigDefaults(ModConfig.Instance);
        }
    }

    public static void SaveModConfig(ICoreAPI api, ModConfig configInstance)
    {
        api.StoreModConfig(configInstance, ModConfig.ConfigPath);
    }

    internal static void UnloadModConfig() => ModConfig.Instance = null;

    private static void MapLegacyData(ModConfig instance)
    {
        if (instance?.LegacyData is null) return;

        var legacyData = new Dictionary<string, JToken>(instance.LegacyData, StringComparer.OrdinalIgnoreCase);
        var subConfigs = typeof(ModConfig)
            .GetProperties()
            .Where(prop => prop.PropertyType.IsClass)
            .Select(prop => prop.GetValue(instance))
            .Where(v => v is not null);

        // Auto fields
        foreach (var subConfig in subConfigs)
        {
            foreach (var property in subConfig!.GetType().GetProperties())
            {
                if (legacyData.TryGetValue(property.Name, out var token))
                {
                    try
                    {
                        property.SetValue(subConfig, token.ToObject(property.PropertyType));
                    }
                    catch
                    {
                        // Ignore unknown legacy data
                    }
                }
            }
        }
        instance.LegacyData = null;
    }

    private static bool TryGetBool(Dictionary<string, JToken> legacyData, string key, out bool result)
    {
        if (legacyData.TryGetValue(key, out var boolToken) && boolToken.Type == JTokenType.Boolean)
        {
            result = boolToken.ToObject<bool>();
            return true;
        }

        result = false;
        return false;
    }
    private static void EnsureSubconfigDefaults(ModConfig cfg)
    {
        if (cfg.Rewards is null || IsRewardsUninitialized(cfg.Rewards))
        {
            cfg.Rewards = LockpickDefaults.Create();
        }
        cfg.Rewards.Easy   ??= new TierLootConfig();
        cfg.Rewards.Medium ??= new TierLootConfig();
        cfg.Rewards.Hard   ??= new TierLootConfig();
        cfg.Rewards.Brutal ??= new TierLootConfig();

        cfg.Rewards.Easy.Pool   ??= new List<LootPoolEntry>();
        cfg.Rewards.Medium.Pool ??= new List<LootPoolEntry>();
        cfg.Rewards.Hard.Pool   ??= new List<LootPoolEntry>();
        cfg.Rewards.Brutal.Pool ??= new List<LootPoolEntry>();
    }

    private static bool IsRewardsUninitialized(LockpickRewardsConfig r)
    {
        bool poolEmpty(List<LootPoolEntry> p) => p == null || p.Count == 0;
        return r.Easy   == null && r.Medium == null && r.Hard == null && r.Brutal == null
               || (r.Easy   != null && r.Medium != null && r.Hard != null && r.Brutal != null
                   && poolEmpty(r.Easy.Pool)
                   && poolEmpty(r.Medium.Pool)
                   && poolEmpty(r.Hard.Pool)
                   && poolEmpty(r.Brutal.Pool));
    }
}
