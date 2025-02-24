using ConfigLib;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Thievery.Config
{
    public class ConfigLibCompatibility
    {
        private const string settingLockPicking = "thievery:Config.Setting.LockPicking";
        private const string settingLockPickDamageChance = "thievery:Config.Setting.LockPickDamageChance";
        private const string settingLockPickDamage = "thievery:Config.Setting.LockPickDamage";
        private const string settingRequiresPilferer = "thievery:Config.Setting.RequiresPilferer";
        private const string settingRequiresTinkerer = "thievery:Config.Setting.RequiresTinkerer";
        private const string settingStructureLockChance = "thievery:Config.Setting.StructureLockChance";
        private const string settingStructureMinReinforcement = "thievery:Config.Setting.StructureMinReinforcement";
        private const string settingStructureMaxReinforcement = "thievery:Config.Setting.StructureMaxReinforcement";
        private const string settingReinforcedBuildingBlocks = "thievery:Config.Setting.ReinforcedBuildingBlocks";
        private const string settingBlackBronzePickDuration = "thievery:Config.Setting.BlackBronzePadlockPickDuration";
        private const string settingBismuthBronzePickDuration = "thievery:Config.Setting.BismuthBronzePadlockPickDuration";
        private const string settingTinBronzePickDuration = "thievery:Config.Setting.TinBronzePadlockPickDuration";
        private const string settingIronPickDuration = "thievery:Config.Setting.IronPadlockPickDuration";
        private const string settingMeteoricIronPickDuration = "thievery:Config.Setting.MeteoricIronPadlockPickDuration";
        private const string settingSteelPickDuration = "thievery:Config.Setting.SteelPadlockPickDuration";
        private const string settingCopperPickDuration = "thievery:Config.Setting.CopperPadlockPickDuration";
        private const string settingNickelPickDuration = "thievery:Config.Setting.NickelPadlockPickDuration";
        private const string settingLeadPickDuration = "thievery:Config.Setting.LeadPadlockPickDuration";
        private const string settingTinPickDuration = "thievery:Config.Setting.TinPadlockPickDuration";
        private const string settingZincPickDuration = "thievery:Config.Setting.ZincPadlockPickDuration";
        private const string settingCupronickelPickDuration = "thievery:Config.Setting.CupronickelPadlockPickDuration";
        private const string settingChromiumPickDuration = "thievery:Config.Setting.ChromiumPadlockPickDuration";
        private const string settingTitaniumPickDuration = "thievery:Config.Setting.TitaniumPadlockPickDuration";
        private const string settingSilverPickDuration = "thievery:Config.Setting.SilverPadlockPickDuration";
        private const string settingElectrumPickDuration = "thievery:Config.Setting.ElectrumPadlockPickDuration";
        private const string settingGoldPickDuration = "thievery:Config.Setting.GoldPadlockPickDuration";
        private const string settingPlatinumPickDuration = "thievery:Config.Setting.PlatinumPadlockPickDuration";
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
            
            int structureMinReinforcement = config.StructureMinReinforcement;
            ImGui.DragInt(Lang.Get(settingStructureMinReinforcement) + $"##structureMinReinforcement-{id}", ref structureMinReinforcement, 1, 1, 1000);
            config.StructureMinReinforcement = structureMinReinforcement;
            
            int structureMaxReinforcement = config.StructureMaxReinforcement;
            ImGui.DragInt(Lang.Get(settingStructureMaxReinforcement) + $"##structureMaxReinforcement-{id}", ref structureMaxReinforcement, 1, 1, 2000);
            config.StructureMaxReinforcement = structureMaxReinforcement;
            
            bool reinforcedBuildingBlocks = config.ReinforcedBuildingBlocks;
            ImGui.Checkbox(Lang.Get(settingReinforcedBuildingBlocks) + $"##reinforcedBuildingBlocks-{id}", ref reinforcedBuildingBlocks);
            config.ReinforcedBuildingBlocks = reinforcedBuildingBlocks;
            
            float structureLockChance = config.StructureLockChance;
            ImGui.DragFloat(Lang.Get(settingStructureLockChance) + $"##structureLockChance-{id}", ref structureLockChance, 0.01f, 0.0f, 1.0f);
            config.StructureLockChance = structureLockChance;

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

            float copperDurationSeconds = config.CopperPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingCopperPickDuration) + $"##copperDuration-{id}", ref copperDurationSeconds, 1.0f, 1.0f, 400.0f);
            config.CopperPadlockPickDurationSeconds = copperDurationSeconds;

            float nickelDurationSeconds = config.NickelPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingNickelPickDuration) + $"##nickelDuration-{id}", ref nickelDurationSeconds, 1.0f, 1.0f, 400.0f);
            config.NickelPadlockPickDurationSeconds = nickelDurationSeconds;

            float leadDurationSeconds = config.LeadPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingLeadPickDuration) + $"##leadDuration-{id}", ref leadDurationSeconds, 1.0f, 1.0f, 400.0f);
            config.LeadPadlockPickDurationSeconds = leadDurationSeconds;

            float tinDurationSeconds = config.TinPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingTinPickDuration) + $"##tinDuration-{id}", ref tinDurationSeconds, 1.0f, 1.0f, 400.0f);
            config.TinPadlockPickDurationSeconds = tinDurationSeconds;

            float zincDurationSeconds = config.ZincPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingZincPickDuration) + $"##zincDuration-{id}", ref zincDurationSeconds, 1.0f, 1.0f, 400.0f);
            config.ZincPadlockPickDurationSeconds = zincDurationSeconds;

            float cupronickelDurationSeconds = config.CupronickelPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingCupronickelPickDuration) + $"##cupronickelDuration-{id}", ref cupronickelDurationSeconds, 1.0f, 1.0f, 400.0f);
            config.CupronickelPadlockPickDurationSeconds = cupronickelDurationSeconds;

            float chromiumDurationSeconds = config.ChromiumPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingChromiumPickDuration) + $"##chromiumDuration-{id}", ref chromiumDurationSeconds, 1.0f, 1.0f, 400.0f);
            config.ChromiumPadlockPickDurationSeconds = chromiumDurationSeconds;

            float titaniumDurationSeconds = config.TitaniumPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingTitaniumPickDuration) + $"##titaniumDuration-{id}", ref titaniumDurationSeconds, 1.0f, 1.0f, 400.0f);
            config.TitaniumPadlockPickDurationSeconds = titaniumDurationSeconds;

            float silverDurationSeconds = config.SilverPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingSilverPickDuration) + $"##silverDuration-{id}", ref silverDurationSeconds, 1.0f, 1.0f, 400.0f);
            config.SilverPadlockPickDurationSeconds = silverDurationSeconds;

            float electrumDurationSeconds = config.ElectrumPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingElectrumPickDuration) + $"##electrumDuration-{id}", ref electrumDurationSeconds, 1.0f, 1.0f, 400.0f);
            config.ElectrumPadlockPickDurationSeconds = electrumDurationSeconds;

            float goldDurationSeconds = config.GoldPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingGoldPickDuration) + $"##goldDuration-{id}", ref goldDurationSeconds, 1.0f, 1.0f, 400.0f);
            config.GoldPadlockPickDurationSeconds = goldDurationSeconds;

            float platinumDurationSeconds = config.PlatinumPadlockPickDurationSeconds;
            ImGui.DragFloat(Lang.Get(settingPlatinumPickDuration) + $"##platinumDuration-{id}", ref platinumDurationSeconds, 1.0f, 1.0f, 400.0f);
            config.PlatinumPadlockPickDurationSeconds = platinumDurationSeconds;
        }
    }
}
