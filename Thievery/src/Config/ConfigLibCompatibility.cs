using ConfigLib;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Thievery.Config
{
    public class ConfigLibCompatibility
    {
        private const string settingLockPicking = "thievery:Config.Setting.LockPicking";
        private const string settingBlackBronzePickDuration = "thievery:Config.Setting.BlackBronzePadlockPickDuration";
        private const string settingBismuthBronzePickDuration = "thievery:Config.Setting.BismuthBronzePadlockPickDuration";
        private const string settingTinBronzePickDuration = "thievery:Config.Setting.TinBronzePadlockPickDuration";
        private const string settingIronPickDuration = "thievery:Config.Setting.IronPadlockPickDuration";
        private const string settingMeteoricIronPickDuration = "thievery:Config.Setting.MeteoricIronPadlockPickDuration";
        private const string settingSteelPickDuration = "thievery:Config.Setting.SteelPadlockPickDuration";
        private const string settingLockPickDamageChance = "thievery:Config.Setting.LockPickDamageChance";
        private const string settingLockPickDamage = "thievery:Config.Setting.LockPickDamage";
        private const string settingRequiresPilferer = "thievery:Config.Setting.RequiresPilferer";
        private const string settingRequiresTinkerer = "thievery:Config.Setting.RequiresTinkerer";

        public ConfigLibCompatibility(ICoreClientAPI api)
        {
            if (!api.ModLoader.IsModEnabled("configlib"))
            {
                return;
            }

            Init(api);
        }

        private void Init(ICoreClientAPI api)
        {
            if (!api.ModLoader.IsModEnabled("configlib"))
            {
                return;
            }

            api.ModLoader.GetModSystem<ConfigLibModSystem>().RegisterCustomConfig("thievery", (id, buttons) => EditConfig(id, buttons, api));
        }

        private void EditConfig(string id, ControlButtons buttons, ICoreClientAPI api)
        {
            if (buttons.Save) ModConfig.WriteConfig(api, "ThieveryConfig.json", ThieveryModSystem.LoadedConfig);
            if (buttons.Defaults) ThieveryModSystem.LoadedConfig = new Config();
            Edit(api, ThieveryModSystem.LoadedConfig, id);
        }

        private void Edit(ICoreClientAPI api, Config config, string id)
        {
            ImGui.TextWrapped("Thievery Settings");

            // Lock Picking Settings
            ImGui.SeparatorText("Lock Picking Settings");

            bool lockPicking = config.LockPicking;
            ImGui.Checkbox(Lang.Get(settingLockPicking) + $"##lockPicking-{id}", ref lockPicking);
            config.LockPicking = lockPicking;

            float blackBronzeDurationSeconds = config.BlackBronzePadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingBlackBronzePickDuration) + $"##blackBronzeDuration-{id}", ref blackBronzeDurationSeconds, 1.0f, 1.0f, 300.0f);
            config.BlackBronzePadlockPickDurationSeconds = blackBronzeDurationSeconds;

            float bismuthBronzeDurationSeconds = config.BismuthBronzePadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingBismuthBronzePickDuration) + $"##bismuthBronzeDuration-{id}", ref bismuthBronzeDurationSeconds, 1.0f, 1.0f, 300.0f);
            config.BismuthBronzePadlockPickDurationSeconds = bismuthBronzeDurationSeconds;

            float tinBronzeDurationSeconds = config.TinBronzePadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingTinBronzePickDuration) + $"##tinBronzeDuration-{id}", ref tinBronzeDurationSeconds, 0.1f, 1.0f, 300.0f);
            config.TinBronzePadlockPickDurationSeconds = tinBronzeDurationSeconds;

            float ironDurationSeconds = config.IronPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingIronPickDuration) + $"##ironDuration-{id}", ref ironDurationSeconds, 1.0f, 1.0f, 300.0f);
            config.IronPadlockPickDurationSeconds = ironDurationSeconds;

            float meteoricIronDurationSeconds = config.MeteoricIronPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingMeteoricIronPickDuration) + $"##meteoricIronDuration-{id}", ref meteoricIronDurationSeconds, 1.0f, 1.0f, 300.0f);
            config.MeteoricIronPadlockPickDurationSeconds = meteoricIronDurationSeconds;

            float steelDurationSeconds = config.SteelPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingSteelPickDuration) + $"##steelDuration-{id}", ref steelDurationSeconds, 1.0f, 1.0f, 300.0f);
            config.SteelPadlockPickDurationSeconds = steelDurationSeconds;

            float lockPickDamageChance = (float)config.LockPickDamageChance;
            ImGui.DragFloat(Lang.Get(settingLockPickDamageChance) + $"##lockPickDamageChance-{id}", ref lockPickDamageChance, 0.01f, 0.0f, 1.0f);
            config.LockPickDamageChance = lockPickDamageChance;

            float lockPickDamage = config.LockPickDamage;
            ImGui.DragFloat(Lang.Get(settingLockPickDamage) + $"##lockPickDamage-{id}", ref lockPickDamage, 10.0f, 0.0f, 1000.0f);
            config.LockPickDamage = lockPickDamage;
            
            bool requiresPilferer = config.RequiresPilferer;
            ImGui.Checkbox(Lang.Get(settingRequiresPilferer) + $"##requiresPilferer-{id}", ref requiresPilferer);
            config.RequiresPilferer = requiresPilferer;
            
            bool requiresTinkerer = config.RequiresTinkerer;
            ImGui.Checkbox(Lang.Get(settingRequiresTinkerer) + $"##requiresTinkerer-{id}", ref requiresTinkerer);
            config.RequiresTinkerer = requiresTinkerer;
        }
    }
}
