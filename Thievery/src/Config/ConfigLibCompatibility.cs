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
        private const string settingReinforceAllBlocks = "thievery:Config.Setting.ReinforceAllBlocks";
        private const string settingBlackBronzePickDifficulty = "thievery:Config.Setting.BlackBronzePadlockPickDifficulty";
        private const string settingBismuthBronzePickDifficulty = "thievery:Config.Setting.BismuthBronzePadlockPickDifficulty";
        private const string settingTinBronzePickDifficulty = "thievery:Config.Setting.TinBronzePadlockPickDifficulty";
        private const string settingIronPickDifficulty = "thievery:Config.Setting.IronPadlockPickDifficulty";
        private const string settingMeteoricIronPickDifficulty = "thievery:Config.Setting.MeteoricIronPadlockPickDifficulty";
        private const string settingSteelPickDifficulty = "thievery:Config.Setting.SteelPadlockPickDifficulty";
        private const string settingCopperPickDifficulty = "thievery:Config.Setting.CopperPadlockPickDifficulty";
        private const string settingNickelPickDifficulty = "thievery:Config.Setting.NickelPadlockPickDifficulty";
        private const string settingLeadPickDifficulty = "thievery:Config.Setting.LeadPadlockPickDifficulty";
        private const string settingTinPickDifficulty = "thievery:Config.Setting.TinPadlockPickDifficulty";
        private const string settingZincPickDifficulty = "thievery:Config.Setting.ZincPadlockPickDifficulty";
        private const string settingCupronickelPickDifficulty = "thievery:Config.Setting.CupronickelPadlockPickDifficulty";
        private const string settingChromiumPickDifficulty = "thievery:Config.Setting.ChromiumPadlockPickDifficulty";
        private const string settingTitaniumPickDifficulty = "thievery:Config.Setting.TitaniumPadlockPickDifficulty";
        private const string settingSilverPickDifficulty = "thievery:Config.Setting.SilverPadlockPickDifficulty";
        private const string settingElectrumPickDifficulty = "thievery:Config.Setting.ElectrumPadlockPickDifficulty";
        private const string settingGoldPickDifficulty = "thievery:Config.Setting.GoldPadlockPickDifficulty";
        private const string settingPlatinumPickDifficulty = "thievery:Config.Setting.PlatinumPadlockPickDifficulty";
        private const string settingBindingOrderDifficultyThreshold = "thievery:Config.Setting.BindingOrderDifficultyThreshold";
        private const string settingAgedKeyDamageChance = "thievery:Config.Setting.AgedKeyDamageChance";
        private const string settingAgedKeyDamage = "thievery:Config.Setting.AgedKeyDamage";
        private const string settingStructureKeyChance = "thievery:Config.Setting.StructureKeyChance";
        private const string settingLockToolDamage = "thievery:Config.Setting.settingLockToolDamage";
        private const string settingOwnerExempt = "thievery:Config.Setting.settingOwnerExempt";

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
            
            bool reinforceAllBlocks = config.ReinforceAllBlocks;
            ImGui.Checkbox(Lang.Get(settingReinforceAllBlocks) + $"##reinforceAllBlocks-{id}", ref reinforceAllBlocks);
            config.ReinforceAllBlocks = reinforceAllBlocks;
            
            float structureLockChance = config.StructureLockChance;
            ImGui.DragFloat(Lang.Get(settingStructureLockChance) + $"##structureLockChance-{id}", ref structureLockChance, 0.01f, 0.0f, 1.0f);
            config.StructureLockChance = structureLockChance;

            int blackBronzePadlockDifficulty = config.BlackBronzePadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingBlackBronzePickDifficulty) + $"##blackBronzeDifficulty-{id}", ref blackBronzePadlockDifficulty, 1.0f, 1, 100);
            config.BlackBronzePadlockDifficulty = blackBronzePadlockDifficulty;

            int bismuthBronzePadlockDifficulty = config.BismuthBronzePadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingBismuthBronzePickDifficulty) + $"##bismuthBronzeDifficulty-{id}", ref bismuthBronzePadlockDifficulty, 1.0f, 1, 100);
            config.BismuthBronzePadlockDifficulty = bismuthBronzePadlockDifficulty;

            int tinBronzePadlockDifficulty = config.TinBronzePadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingTinBronzePickDifficulty) + $"##tinBronzeDifficulty-{id}", ref tinBronzePadlockDifficulty, 0.1f, 1, 100);
            config.TinBronzePadlockDifficulty = tinBronzePadlockDifficulty;

            int ironPadlockDifficulty = config.IronPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingIronPickDifficulty) + $"##ironDifficulty-{id}", ref ironPadlockDifficulty, 1.0f, 1, 100);
            config.IronPadlockDifficulty = ironPadlockDifficulty;

            int meteoricIronPadlockDifficulty = config.MeteoricIronPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingMeteoricIronPickDifficulty) + $"##meteoricIronDifficulty-{id}", ref meteoricIronPadlockDifficulty, 1.0f, 1, 100);
            config.MeteoricIronPadlockDifficulty = meteoricIronPadlockDifficulty;

            int steelPadlockDifficulty = config.SteelPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingSteelPickDifficulty) + $"##steelDifficulty-{id}", ref steelPadlockDifficulty, 1.0f, 1, 100);
            config.SteelPadlockDifficulty = steelPadlockDifficulty;

            int copperPadlockDifficulty = config.CopperPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingCopperPickDifficulty) + $"##copperDifficulty-{id}", ref copperPadlockDifficulty, 1.0f, 1, 100);
            config.CopperPadlockDifficulty = copperPadlockDifficulty;

            int nickelPadlockDifficulty = config.NickelPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingNickelPickDifficulty) + $"##nickelDifficulty-{id}", ref nickelPadlockDifficulty, 1.0f, 1, 100);
            config.NickelPadlockDifficulty = nickelPadlockDifficulty;

            int leadPadlockDifficulty = config.LeadPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingLeadPickDifficulty) + $"##leadDifficulty-{id}", ref leadPadlockDifficulty, 1.0f, 1, 100);
            config.LeadPadlockDifficulty = leadPadlockDifficulty;

            int tinPadlockDifficulty = config.TinPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingTinPickDifficulty) + $"##tinDifficulty-{id}", ref tinPadlockDifficulty, 1.0f, 1, 100);
            config.TinPadlockDifficulty = tinPadlockDifficulty;

            int zincPadlockDifficulty = config.ZincPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingZincPickDifficulty) + $"##zincDifficulty-{id}", ref zincPadlockDifficulty, 1.0f, 1, 100);
            config.ZincPadlockDifficulty = zincPadlockDifficulty;

            int cupronickelPadlockDifficulty = config.CupronickelPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingCupronickelPickDifficulty) + $"##cupronickelDifficulty-{id}", ref cupronickelPadlockDifficulty, 1.0f, 1, 100);
            config.CupronickelPadlockDifficulty = cupronickelPadlockDifficulty;

            int chromiumPadlockDifficulty = config.ChromiumPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingChromiumPickDifficulty) + $"##chromiumDifficulty-{id}", ref chromiumPadlockDifficulty, 1.0f, 1, 100);
            config.ChromiumPadlockDifficulty = chromiumPadlockDifficulty;

            int titaniumPadlockDifficulty = config.TitaniumPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingTitaniumPickDifficulty) + $"##titaniumDifficulty-{id}", ref titaniumPadlockDifficulty, 1.0f, 1, 100);
            config.TitaniumPadlockDifficulty = titaniumPadlockDifficulty;

            int silverPadlockDifficulty = config.SilverPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingSilverPickDifficulty) + $"##silverDifficulty-{id}", ref silverPadlockDifficulty, 1.0f, 1, 100);
            config.SilverPadlockDifficulty = silverPadlockDifficulty;

            int electrumPadlockDifficulty = config.ElectrumPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingElectrumPickDifficulty) + $"##electrumDifficulty-{id}", ref electrumPadlockDifficulty, 1.0f, 1, 100);
            config.ElectrumPadlockDifficulty = electrumPadlockDifficulty;

            int goldPadlockDifficulty = config.GoldPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingGoldPickDifficulty) + $"##goldDifficulty-{id}", ref goldPadlockDifficulty, 1.0f, 1, 100);
            config.GoldPadlockDifficulty = goldPadlockDifficulty;

            int platinumPadlockDifficulty = config.PlatinumPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingPlatinumPickDifficulty) + $"##platinumDifficulty-{id}", ref platinumPadlockDifficulty, 1.0f, 1, 100);
            config.PlatinumPadlockDifficulty = platinumPadlockDifficulty;

            int bindingOrderDifficultyThreshold = config.MinigameBindingOrderThreshold;
            ImGui.DragInt(Lang.Get(settingBindingOrderDifficultyThreshold) + $"##bindingOrderDifficultyThreshold-{id}", ref bindingOrderDifficultyThreshold, 1.0f, 1, 100);
            config.MinigameBindingOrderThreshold = bindingOrderDifficultyThreshold;

            ImGui.SeparatorText("Key Generation Settings");

            float agedKeyDamageChance = config.AgedKeyDamageChance;
            ImGui.DragFloat(Lang.Get(settingAgedKeyDamageChance) + $"##agedKeyDamageChance-{id}", ref agedKeyDamageChance, 0.01f, 0.0f, 1.0f);
            config.AgedKeyDamageChance = agedKeyDamageChance;

            int agedKeyDamage = config.AgedKeyDamage;
            ImGui.DragInt(Lang.Get(settingAgedKeyDamage) + $"##agedKeyDamage-{id}", ref agedKeyDamage, 1, 0, 100);
            config.AgedKeyDamage = agedKeyDamage;

            float structureKeyChance = (float)config.StructureKeyChance;
            ImGui.DragFloat(Lang.Get(settingStructureKeyChance) + $"##structureKeyChance-{id}", ref structureKeyChance, 0.01f, 0.0f, 1.0f);
            config.StructureKeyChance = structureKeyChance;
            
            int lockToolDamage = config.LockToolDamage;
            ImGui.DragInt(Lang.Get(settingLockToolDamage) + $"##lockToolDamage-{id}", ref lockToolDamage, 1, 0, 100);
            config.LockToolDamage = lockToolDamage;
            
            bool ownerExempt = config.OwnerExempt;
            ImGui.Checkbox(Lang.Get(settingOwnerExempt) + $"##ownerExempt-{id}", ref ownerExempt);
            config.OwnerExempt = ownerExempt;
        }
    }
}
