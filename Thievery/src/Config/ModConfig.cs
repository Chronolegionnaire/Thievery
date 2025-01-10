using System;
using Vintagestory.API.Common;

namespace Thievery.Config;

public static class ModConfig
    {
        public static T ReadConfig<T>(ICoreAPI api, string jsonConfig) where T : class, IModConfig
        {
            T config = api.LoadModConfig<T>(jsonConfig);
            if (config == null)
            {
                config = Activator.CreateInstance<T>();
                if (config is Config thieveryConfig)
                {
                    thieveryConfig.LockPicking = true;
                    thieveryConfig.BlackBronzePadlockPickDurationSeconds = 20;
                    thieveryConfig.BismuthBronzePadlockPickDurationSeconds = 24;
                    thieveryConfig.TinBronzePadlockPickDurationSeconds = 28;
                    thieveryConfig.IronPadlockPickDurationSeconds = 60;
                    thieveryConfig.MeteoricIronPadlockPickDurationSeconds = 100;
                    thieveryConfig.SteelPadlockPickDurationSeconds = 180;
                    thieveryConfig.LockPickDamageChance = 0.1;
                    thieveryConfig.LockPickDamage = 200;
                    thieveryConfig.RequiresPilferer = true;
                    thieveryConfig.RequiresTinkerer = true;
                }
                WriteConfig(api, jsonConfig, config);
            }
            return config;
        }

        public static void WriteConfig<T>(ICoreAPI api, string jsonConfig, T config) where T : class, IModConfig
        {
            api.StoreModConfig(config, jsonConfig);
        }
    }