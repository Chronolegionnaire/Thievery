using ConfigLib;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thievery.Config.SubConfigs;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Thievery.Config
{
    public class ConfigLibCompatibility
    {
        // ──────────────────────────────────────────────────────────────────────
        // Lang keys
        // ──────────────────────────────────────────────────────────────────────

        // LockpickingMain
        private const string settingLockPicking                        = "thievery:Config.Setting.LockPicking";
        private const string settingLockPickDamageChance               = "thievery:Config.Setting.LockPickDamageChance";
        private const string settingLockPickDamage                     = "thievery:Config.Setting.LockPickDamage";
        private const string settingRequiredTraits                     = "thievery:Config.Setting.RequiredTraits";
        private const string settingAgedKeyDamageChance                = "thievery:Config.Setting.AgedKeyDamageChance";
        private const string settingAgedKeyDamage                      = "thievery:Config.Setting.AgedKeyDamage";
        private const string settingLockToolDamage                     = "thievery:Config.Setting.LockToolDamage";
        private const string settingOwnerExempt                        = "thievery:Config.Setting.OwnerExempt";
        private const string settingBlockLockpickOnLandClaims          = "thievery:Config.Setting.BlockLockpickOnLandClaims";

        // LockpickingMiniGame
        private const string settingLockpickingMinigame                = "thievery:Config.Setting.LockpickingMinigame";
        private const string settingMiniLockpickDamageBase             = "thievery:Config.Setting.Mini.LockpickDamageBase";
        private const string settingMiniTensionWrenchDamageBase        = "thievery:Config.Setting.Mini.TensionWrenchDamageBase";
        private const string settingMiniBindingOrderThreshold          = "thievery:Config.Setting.Mini.BindingOrderThreshold";
        private const string settingMiniHotspotForgivenessBins          = "thievery:Config.Setting.Mini.HotspotForgivenessBins";
        private const string settingMiniProbeLockoutMinutes        = "thievery:Config.Setting.Mini.ProbeLockoutMinutes";
        private const string settingMiniProbeBreakChanceIncrement  = "thievery:Config.Setting.Mini.ProbeBreakChanceIncrement";
        private const string settingMiniProbeFreeUses              = "thievery:Config.Setting.Mini.ProbeFreeUses";
        private const string settingMiniPermanentLockBreak         = "thievery:Config.Setting.Mini.PermanentLockBreak";


        // LockDifficulty (per material)
        private const string settingDiffCopper                         = "thievery:Config.Setting.Difficulty.Copper";
        private const string settingDiffNickel                         = "thievery:Config.Setting.Difficulty.Nickel";
        private const string settingDiffLead                           = "thievery:Config.Setting.Difficulty.Lead";
        private const string settingDiffTin                            = "thievery:Config.Setting.Difficulty.Tin";
        private const string settingDiffZinc                           = "thievery:Config.Setting.Difficulty.Zinc";
        private const string settingDiffTinBronze                      = "thievery:Config.Setting.Difficulty.TinBronze";
        private const string settingDiffBismuthBronze                  = "thievery:Config.Setting.Difficulty.BismuthBronze";
        private const string settingDiffBlackBronze                    = "thievery:Config.Setting.Difficulty.BlackBronze";
        private const string settingDiffCupronickel                    = "thievery:Config.Setting.Difficulty.Cupronickel";
        private const string settingDiffIron                           = "thievery:Config.Setting.Difficulty.Iron";
        private const string settingDiffMeteoricIron                   = "thievery:Config.Setting.Difficulty.MeteoricIron";
        private const string settingDiffSteel                          = "thievery:Config.Setting.Difficulty.Steel";
        private const string settingDiffSilver                         = "thievery:Config.Setting.Difficulty.Silver";
        private const string settingDiffElectrum                       = "thievery:Config.Setting.Difficulty.Electrum";
        private const string settingDiffGold                           = "thievery:Config.Setting.Difficulty.Gold";
        private const string settingDiffPlatinum                       = "thievery:Config.Setting.Difficulty.Platinum";
        private const string settingDiffChromium                       = "thievery:Config.Setting.Difficulty.Chromium";
        private const string settingDiffTitanium                       = "thievery:Config.Setting.Difficulty.Titanium";

        // WorldGenLockingAndReinforcement
        private const string settingStructureLockChance                = "thievery:Config.Setting.StructureLockChance";
        private const string settingStructureKeyChance                 = "thievery:Config.Setting.StructureKeyChance";
        private const string settingStructureMinReinforcement          = "thievery:Config.Setting.StructureMinReinforcement";
        private const string settingStructureMaxReinforcement          = "thievery:Config.Setting.StructureMaxReinforcement";
        private const string settingReinforcedBuildingBlocks           = "thievery:Config.Setting.ReinforcedBuildingBlocks";
        private const string settingReinforceAllBlocks                 = "thievery:Config.Setting.ReinforceAllBlocks";

        // StructureBlacklist
        private const string settingStructureBlacklist                 = "thievery:Config.Setting.StructureBlacklist";
        
        // Rewards – main
        private const string settingRewardsEnabled            = "thievery:Config.Setting.Rewards.Enabled";

        // Rewards – tier headings
        private const string settingTierEasy                  = "thievery:Config.Setting.Rewards.Tier.Easy";
        private const string settingTierMedium                = "thievery:Config.Setting.Rewards.Tier.Medium";
        private const string settingTierHard                  = "thievery:Config.Setting.Rewards.Tier.Hard";
        private const string settingTierBrutal                = "thievery:Config.Setting.Rewards.Tier.Brutal";

        // Rewards – tier fields
        private const string settingTierRolls                 = "thievery:Config.Setting.Rewards.Tier.Rolls";
        private const string settingTierEmptyWeight           = "thievery:Config.Setting.Rewards.Tier.EmptyWeight";
        private const string settingTierGearsMin              = "thievery:Config.Setting.Rewards.Tier.GearsMin";
        private const string settingTierGearsMax              = "thievery:Config.Setting.Rewards.Tier.GearsMax";

        // Rewards – pool editor
        private const string settingTierPoolHeader            = "thievery:Config.Setting.Rewards.Tier.PoolHeader";
        private const string settingPoolCode                  = "thievery:Config.Setting.Rewards.Pool.Code";
        private const string settingPoolMin                   = "thievery:Config.Setting.Rewards.Pool.Min";
        private const string settingPoolMax                   = "thievery:Config.Setting.Rewards.Pool.Max";
        private const string settingPoolWeight                = "thievery:Config.Setting.Rewards.Pool.Weight";
        private const string settingPoolAddEntry              = "thievery:Config.Setting.Rewards.Pool.AddEntry";
        private const string settingPoolRemove                = "thievery:Config.Setting.Rewards.Pool.Remove";
        private const string settingPoolMoveUp                = "thievery:Config.Setting.Rewards.Pool.MoveUp";
        private const string settingPoolMoveDown              = "thievery:Config.Setting.Rewards.Pool.MoveDown";


        private ConfigLibCompatibility() {}
        public ModConfig EditInstance { get; private set; }

        private static ModConfig LoadFromDisk(ICoreAPI api)
        {
            try
            {
                return api.LoadModConfig<ModConfig>(ModConfig.ConfigPath) ?? new ModConfig();
            }
            catch (Exception ex)
            {
                api.Logger.Error(ex);
                return new ModConfig();
            }
        }

        internal static void Init(ICoreClientAPI api)
        {
            var container = new ConfigLibCompatibility();
            api.ModLoader.GetModSystem<ConfigLibModSystem>().RegisterCustomConfig("thievery", (id, buttons) =>
            {
                container.EditConfig(id, buttons, api);
                return new ControlButtons
                {
                    Save = true,
                    Restore = true,
                    Defaults = true,
                    Reload = api.IsSinglePlayer
                };
            });
        }

        private void EditConfig(string id, ControlButtons buttons, ICoreClientAPI api)
        {
            EditInstance ??= ModConfig.Instance.JsonCopy();

            Edit(EditInstance, id);

            if (buttons.Save) ConfigManager.SaveModConfig(api, EditInstance);
            else if (buttons.Restore) EditInstance = LoadFromDisk(api);
            else if (buttons.Defaults) EditInstance = new ModConfig();
            else if (buttons.Reload)
            {
                if (api.IsSinglePlayer)
                {
                    ModConfig.Instance = EditInstance;
                    EditInstance = null;
                    ConfigManager.StoreModConfigToWorldConfig(api);
                }
                else
                {
                }
            }
        }

        private static void Edit(ModConfig config, string id)
        {
            var main   = config.Main;
            var diff   = config.Difficulty;
            var mini   = config.MiniGame;
            var world  = config.WorldGen;
            var sb     = config.Blacklist;
            var rewards = config.Rewards ??= LockpickDefaults.Create();
            ImGui.TextWrapped("Thievery Settings");

            // ──────────────────────────────────────────────────────────────────
            // Lockpicking – Main
            // ──────────────────────────────────────────────────────────────────
            ImGui.SeparatorText("Lockpicking – Main");

            bool lockPicking = main.LockPicking;
            ImGui.Checkbox(Lang.Get(settingLockPicking) + $"##lockPicking-{id}", ref lockPicking);
            main.LockPicking = lockPicking;

            double lpDmgChance = main.LockPickDamageChance;
            float lpDmgChanceF = (float)lpDmgChance;
            ImGui.DragFloat(Lang.Get(settingLockPickDamageChance) + $"##lockPickDamageChance-{id}", ref lpDmgChanceF, 0.01f, 0.0f, 1.0f);
            main.LockPickDamageChance = Math.Clamp(lpDmgChanceF, 0f, 1f);

            float lpDamage = main.LockPickDamage;
            ImGui.DragFloat(Lang.Get(settingLockPickDamage) + $"##lockPickDamage-{id}", ref lpDamage, 1f, 0f, 10000f);
            main.LockPickDamage = lpDamage;

            string traitList = (main.RequiredTraits != null && main.RequiredTraits.Count > 0)
                ? string.Join("\n", main.RequiredTraits)
                : string.Empty;
            
            var listbuilder = new StringBuilder(traitList, Math.Max(1024, traitList.Length + 256));
            string traittextBuf = listbuilder.ToString();
            ImGui.InputTextMultiline(
                Lang.Get(settingRequiredTraits) + $"##requiredTraits-{id}",
                ref traittextBuf,
                64 * 1024,
                new System.Numerics.Vector2(-1, 200)
            );
            
            var traitlines = traittextBuf
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            main.RequiredTraits = traitlines;

            double agedKeyDmgChance = main.AgedKeyDamageChance;
            float agedKeyDmgChanceF = (float)agedKeyDmgChance;
            ImGui.DragFloat(Lang.Get(settingAgedKeyDamageChance) + $"##agedKeyDamageChance-{id}", ref agedKeyDmgChanceF, 0.01f, 0.0f, 1.0f);
            main.AgedKeyDamageChance = Math.Clamp(agedKeyDmgChanceF, 0f, 1f);

            int agedKeyDmg = main.AgedKeyDamage;
            ImGui.DragInt(Lang.Get(settingAgedKeyDamage) + $"##agedKeyDamage-{id}", ref agedKeyDmg, 1, 0, int.MaxValue);
            main.AgedKeyDamage = Math.Max(0, agedKeyDmg);

            int lockToolDmg = main.LockToolDamage;
            ImGui.DragInt(Lang.Get(settingLockToolDamage) + $"##lockToolDamage-{id}", ref lockToolDmg, 1, 0, int.MaxValue);
            main.LockToolDamage = Math.Max(0, lockToolDmg);

            bool ownerExempt = main.OwnerExempt;
            ImGui.Checkbox(Lang.Get(settingOwnerExempt) + $"##ownerExempt-{id}", ref ownerExempt);
            main.OwnerExempt = ownerExempt;
            
            bool blockLockpickOnLandClaims = main.BlockLockpickOnLandClaims;
            ImGui.Checkbox(Lang.Get(settingBlockLockpickOnLandClaims) + $"##blockLockpickOnLandClaims-{id}", ref blockLockpickOnLandClaims);
            main.BlockLockpickOnLandClaims = blockLockpickOnLandClaims;

            // ──────────────────────────────────────────────────────────────────
            // Lockpicking – Minigame
            // ──────────────────────────────────────────────────────────────────
            ImGui.SeparatorText("Lockpicking – Minigame");
            
            bool minigameEnabled = mini.LockpickingMinigame;
            ImGui.Checkbox(Lang.Get(settingLockpickingMinigame) + $"##lockpickingMinigame-{id}", ref minigameEnabled);
            mini.LockpickingMinigame = minigameEnabled;

            double PickBase = mini.TensionWrenchDamageBase;
            float miniPickBasef = (float)PickBase;
            ImGui.DragFloat(Lang.Get(settingMiniLockpickDamageBase) + $"##miniPickBase-{id}", ref miniPickBasef, 0.1f, 0, int.MaxValue);
            mini.LockpickDamageBase = Math.Max(0, miniPickBasef);
            
            double WrenchBase = mini.TensionWrenchDamageBase;
            float miniWrenchBasef = (float)WrenchBase;
            ImGui.DragFloat(Lang.Get(settingMiniTensionWrenchDamageBase) + $"##miniWrenchBase-{id}", ref miniWrenchBasef, 0.1f, 0, int.MaxValue);
            mini.TensionWrenchDamageBase = Math.Max(0, miniWrenchBasef);

            int bindThreshold = mini.BindingOrderThreshold;
            ImGui.DragInt(Lang.Get(settingMiniBindingOrderThreshold) + $"##bindThreshold-{id}", ref bindThreshold, 1, 0, 100);
            mini.BindingOrderThreshold = Math.Clamp(bindThreshold, 0, 100);
            
            float miniHotspotForgivenessBins = mini.HotspotForgivenessBins;
            ImGui.DragFloat(Lang.Get(settingMiniHotspotForgivenessBins) + $"##miniHotspotForgivenessBins-{id}", ref miniHotspotForgivenessBins, 0.01f, 0f, 0.5f);
            mini.HotspotForgivenessBins = Math.Clamp(miniHotspotForgivenessBins, 0.0f, 0.5f);
            
            int probeLockoutMin = mini.ProbeLockoutMinutes;
            ImGui.DragInt(Lang.Get(settingMiniProbeLockoutMinutes) + $"##probeLockoutMinutes-{id}",
                ref probeLockoutMin, 1, 1, 120);
            mini.ProbeLockoutMinutes = Math.Clamp(probeLockoutMin, 1, 120);
            
            double probeInc = mini.ProbeBreakChanceIncrement;
            float probeIncF = (float)probeInc;
            ImGui.DragFloat(Lang.Get(settingMiniProbeBreakChanceIncrement) + $"##probeBreakInc-{id}",
                ref probeIncF, 0.01f, 0.0f, 1.0f);
            mini.ProbeBreakChanceIncrement = Math.Clamp(probeIncF, 0f, 1f);
            
            int probeFree = mini.ProbeFreeUses;
            ImGui.DragInt(Lang.Get(settingMiniProbeFreeUses) + $"##probeFreeUses-{id}",
                ref probeFree, 1, 0, 10);
            mini.ProbeFreeUses = Math.Clamp(probeFree, 0, 10);
            
            bool permanentBreak = mini.PermanentLockBreak;
            ImGui.Checkbox(Lang.Get(settingMiniPermanentLockBreak) + $"##permanentLockBreak-{id}",
                ref permanentBreak);
            mini.PermanentLockBreak = permanentBreak;
            
            // ──────────────────────────────────────────────────────────────────
            // Lockpicking – Rewards
            // ──────────────────────────────────────────────────────────────────
            ImGui.SeparatorText("Lockpicking – Rewards");

            EditRewards(rewards, id);

            // ──────────────────────────────────────────────────────────────────
            // Lock Difficulty
            // ──────────────────────────────────────────────────────────────────
            ImGui.SeparatorText("Lock Difficulty");

            int copperDiff = diff.CopperPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffCopper) + $"##copper-{id}", ref copperDiff, 1, 0, 100);
            diff.CopperPadlockDifficulty = Math.Clamp(copperDiff, 0, 100);

            int nickelDiff = diff.NickelPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffNickel) + $"##nickel-{id}", ref nickelDiff, 1, 0, 100);
            diff.NickelPadlockDifficulty = Math.Clamp(nickelDiff, 0, 100);

            int leadDiff = diff.LeadPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffLead) + $"##lead-{id}", ref leadDiff, 1, 0, 100);
            diff.LeadPadlockDifficulty = Math.Clamp(leadDiff, 0, 100);

            int tinDiff = diff.TinPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffTin) + $"##tin-{id}", ref tinDiff, 1, 0, 100);
            diff.TinPadlockDifficulty = Math.Clamp(tinDiff, 0, 100);

            int zincDiff = diff.ZincPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffZinc) + $"##zinc-{id}", ref zincDiff, 1, 0, 100);
            diff.ZincPadlockDifficulty = Math.Clamp(zincDiff, 0, 100);

            int tinBronzeDiff = diff.TinBronzePadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffTinBronze) + $"##tinbronze-{id}", ref tinBronzeDiff, 1, 0, 100);
            diff.TinBronzePadlockDifficulty = Math.Clamp(tinBronzeDiff, 0, 100);

            int bismuthBronzeDiff = diff.BismuthBronzePadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffBismuthBronze) + $"##bismuthbronze-{id}", ref bismuthBronzeDiff, 1, 0, 100);
            diff.BismuthBronzePadlockDifficulty = Math.Clamp(bismuthBronzeDiff, 0, 100);

            int blackBronzeDiff = diff.BlackBronzePadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffBlackBronze) + $"##blackbronze-{id}", ref blackBronzeDiff, 1, 0, 100);
            diff.BlackBronzePadlockDifficulty = Math.Clamp(blackBronzeDiff, 0, 100);

            int cupronickelDiff = diff.CupronickelPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffCupronickel) + $"##cupronickel-{id}", ref cupronickelDiff, 1, 0, 100);
            diff.CupronickelPadlockDifficulty = Math.Clamp(cupronickelDiff, 0, 100);

            int ironDiff = diff.IronPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffIron) + $"##iron-{id}", ref ironDiff, 1, 0, 100);
            diff.IronPadlockDifficulty = Math.Clamp(ironDiff, 0, 100);

            int meteoricIronDiff = diff.MeteoricIronPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffMeteoricIron) + $"##meteoriciron-{id}", ref meteoricIronDiff, 1, 0, 100);
            diff.MeteoricIronPadlockDifficulty = Math.Clamp(meteoricIronDiff, 0, 100);

            int steelDiff = diff.SteelPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffSteel) + $"##steel-{id}", ref steelDiff, 1, 0, 100);
            diff.SteelPadlockDifficulty = Math.Clamp(steelDiff, 0, 100);

            int silverDiff = diff.SilverPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffSilver) + $"##silver-{id}", ref silverDiff, 1, 0, 100);
            diff.SilverPadlockDifficulty = Math.Clamp(silverDiff, 0, 100);

            int electrumDiff = diff.ElectrumPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffElectrum) + $"##electrum-{id}", ref electrumDiff, 1, 0, 100);
            diff.ElectrumPadlockDifficulty = Math.Clamp(electrumDiff, 0, 100);

            int goldDiff = diff.GoldPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffGold) + $"##gold-{id}", ref goldDiff, 1, 0, 100);
            diff.GoldPadlockDifficulty = Math.Clamp(goldDiff, 0, 100);

            int platinumDiff = diff.PlatinumPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffPlatinum) + $"##platinum-{id}", ref platinumDiff, 1, 0, 100);
            diff.PlatinumPadlockDifficulty = Math.Clamp(platinumDiff, 0, 100);

            int chromiumDiff = diff.ChromiumPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffChromium) + $"##chromium-{id}", ref chromiumDiff, 1, 0, 100);
            diff.ChromiumPadlockDifficulty = Math.Clamp(chromiumDiff, 0, 100);

            int titaniumDiff = diff.TitaniumPadlockDifficulty;
            ImGui.DragInt(Lang.Get(settingDiffTitanium) + $"##titanium-{id}", ref titaniumDiff, 1, 0, 100);
            diff.TitaniumPadlockDifficulty = Math.Clamp(titaniumDiff, 0, 100);



            // ──────────────────────────────────────────────────────────────────
            // Worldgen Locking & Reinforcement
            // ──────────────────────────────────────────────────────────────────
            ImGui.SeparatorText("Worldgen Locking & Reinforcement");

            double structLockChance = world.StructureLockChance;
            float structLockChanceF = (float)structLockChance;
            ImGui.DragFloat(Lang.Get(settingStructureLockChance) + $"##structureLockChance-{id}", ref structLockChanceF, 0.01f, 0.0f, 1.0f);
            world.StructureLockChance = Math.Clamp(structLockChanceF, 0f, 1f);

            double structKeyChance = world.StructureKeyChance;
            float structKeyChanceF = (float)structKeyChance;
            ImGui.DragFloat(Lang.Get(settingStructureKeyChance) + $"##structureKeyChance-{id}", ref structKeyChanceF, 0.01f, 0.0f, 1.0f);
            world.StructureKeyChance = Math.Clamp(structKeyChanceF, 0f, 1f);

            int minReinf = world.StructureMinReinforcement;
            ImGui.DragInt(Lang.Get(settingStructureMinReinforcement) + $"##structureMinReinforcement-{id}", ref minReinf, 1, 0, int.MaxValue);
            world.StructureMinReinforcement = Math.Max(0, minReinf);

            int maxReinf = world.StructureMaxReinforcement;
            ImGui.DragInt(Lang.Get(settingStructureMaxReinforcement) + $"##structureMaxReinforcement-{id}", ref maxReinf, 1, 0, int.MaxValue);
            world.StructureMaxReinforcement = Math.Max(0, maxReinf);

            bool reinfBlocks = world.ReinforcedBuildingBlocks;
            ImGui.Checkbox(Lang.Get(settingReinforcedBuildingBlocks) + $"##reinforcedBlocks-{id}", ref reinfBlocks);
            world.ReinforcedBuildingBlocks = reinfBlocks;

            bool reinforceAll = world.ReinforceAllBlocks;
            ImGui.Checkbox(Lang.Get(settingReinforceAllBlocks) + $"##reinforceAllBlocks-{id}", ref reinforceAll);
            world.ReinforceAllBlocks = reinforceAll;

            // ──────────────────────────────────────────────────────────────────
            // Structure Blacklist
            // ──────────────────────────────────────────────────────────────────
            ImGui.SeparatorText("Structure Blacklist");
            
            string listText = (sb.StructureBlacklist != null && sb.StructureBlacklist.Count > 0)
                ? string.Join("\n", sb.StructureBlacklist)
                : string.Empty;
            
            var builder = new StringBuilder(listText, Math.Max(1024, listText.Length + 256));
            string textBuf = builder.ToString();
            ImGui.InputTextMultiline(
                Lang.Get(settingStructureBlacklist) + $"##structureBlacklist-{id}",
                ref textBuf,
                64 * 1024,
                new System.Numerics.Vector2(-1, 200)
            );
            
            var lines = textBuf
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            sb.StructureBlacklist = lines;
        }

        private static void DragDiff(string label, ref int value, string id, string suffix)
        {
            int v = value;
            ImGui.DragInt($"{label}##diff-{suffix}-{id}", ref v, 1, 1, 100);
            value = Math.Clamp(v, 1, 100);
        }
        private static void EditRewards(LockpickRewardsConfig rwd, string id)
        {
            // Master enable
            bool enabled = rwd.Enabled;
            ImGui.Checkbox(Lang.Get(settingRewardsEnabled) + $"##rewardsEnabled-{id}", ref enabled);
            rwd.Enabled = enabled;

            ImGui.BeginDisabled(!rwd.Enabled);
            {
                EditTier(Lang.Get(settingTierEasy),   rwd.Easy  ??= new TierLootConfig(), $"easy-{id}");
                EditTier(Lang.Get(settingTierMedium), rwd.Medium??= new TierLootConfig(), $"medium-{id}");
                EditTier(Lang.Get(settingTierHard),   rwd.Hard  ??= new TierLootConfig(), $"hard-{id}");
                EditTier(Lang.Get(settingTierBrutal), rwd.Brutal??= new TierLootConfig(), $"brutal-{id}");
            }
            ImGui.EndDisabled();
        }

        private static void EditTier(string title, TierLootConfig tier, string id)
        {
            if (ImGui.TreeNodeEx($"{title}##tier-{id}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                // Rolls
                int rolls = tier.Rolls;
                ImGui.DragInt(Lang.Get(settingTierRolls) + $"##rolls-{id}", ref rolls, 1, 0, 1000);
                tier.Rolls = Math.Clamp(rolls, 0, 1000);

                // Empty weight
                int empty = tier.EmptyWeight;
                ImGui.DragInt(Lang.Get(settingTierEmptyWeight) + $"##empty-{id}", ref empty, 1, 0, int.MaxValue);
                tier.EmptyWeight = Math.Max(0, empty);

                // Gears (non-container)
                int gmin = tier.GearsMin;
                int gmax = tier.GearsMax;
                ImGui.DragInt(Lang.Get(settingTierGearsMin) + $"##gmin-{id}", ref gmin, 1, 0, int.MaxValue);
                ImGui.DragInt(Lang.Get(settingTierGearsMax) + $"##gmax-{id}", ref gmax, 1, 0, int.MaxValue);
                if (gmax < gmin) gmax = gmin;
                tier.GearsMin = Math.Max(0, gmin);
                tier.GearsMax = Math.Max(0, gmax);

                ImGui.Separator();
                ImGui.TextUnformatted(Lang.Get(settingTierPoolHeader));

                DrawPoolList(tier.Pool ??= new List<LootPoolEntry>(), id);

                ImGui.TreePop();
            }
        }

        private static void DrawPoolList(List<LootPoolEntry> pool, string id)
        {
            // Header row
            ImGui.Columns(6, $"poolcols-{id}", true);
            ImGui.Text(Lang.Get(settingPoolCode));
            ImGui.NextColumn();
            ImGui.Text(Lang.Get(settingPoolMin));
            ImGui.NextColumn();
            ImGui.Text(Lang.Get(settingPoolMax));
            ImGui.NextColumn();
            ImGui.Text(Lang.Get(settingPoolWeight));
            ImGui.NextColumn();
            ImGui.Text("#");
            ImGui.NextColumn();
            ImGui.Text("⇅");
            ImGui.NextColumn();
            ImGui.Separator();

            // Entries
            for (int i = 0; i < pool.Count; i++)
            {
                ImGui.PushID($"pool-{id}-{i}");
                var entry = pool[i];

                // Code
                string code = entry.Code ?? string.Empty;
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("##code", ref code, 1024);
                entry.Code = string.IsNullOrWhiteSpace(code) ? "game:flaxfibers" : code;
                ImGui.NextColumn();

                // Min
                int min = entry.Min;
                ImGui.DragInt("##min", ref min, 1, 0, int.MaxValue);
                entry.Min = Math.Max(0, min);
                ImGui.NextColumn();

                // Max
                int max = entry.Max;
                ImGui.DragInt("##max", ref max, 1, 0, int.MaxValue);
                if (max < entry.Min) max = entry.Min;
                entry.Max = Math.Max(entry.Min, max);
                ImGui.NextColumn();

                // Weight
                int w = entry.Weight;
                ImGui.DragInt("##w", ref w, 1, 0, int.MaxValue);
                entry.Weight = Math.Max(0, w);
                ImGui.NextColumn();

                // Remove
                if (ImGui.SmallButton(Lang.Get(settingPoolRemove)))
                {
                    pool.RemoveAt(i);
                    ImGui.PopID();
                    ImGui.Columns(1);
                    ImGui.Columns(6, $"poolcols-{id}", true);
                    i--;
                    continue;
                }

                ImGui.NextColumn();
                bool canUp = i > 0;
                bool canDn = i < pool.Count - 1;

                if (!canUp) ImGui.BeginDisabled();
                if (ImGui.SmallButton(Lang.Get(settingPoolMoveUp)))
                {
                    (pool[i - 1], pool[i]) = (pool[i], pool[i - 1]);
                }

                if (!canUp) ImGui.EndDisabled();

                ImGui.SameLine();

                if (!canDn) ImGui.BeginDisabled();
                if (ImGui.SmallButton(Lang.Get(settingPoolMoveDown)))
                {
                    (pool[i + 1], pool[i]) = (pool[i], pool[i + 1]);
                }

                if (!canDn) ImGui.EndDisabled();

                ImGui.NextColumn();

                ImGui.PopID();
            }

            ImGui.Columns(1);
            if (ImGui.SmallButton(Lang.Get(settingPoolAddEntry) + $"##add-{id}"))
            {
                pool.Add(new LootPoolEntry());
            }
        }
    }
}
