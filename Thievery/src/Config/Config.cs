using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;

namespace Thievery.Config
{
    [ProtoContract]
    public class Config : IModConfig
    {
        [ProtoMember(1, IsRequired = true)] public bool LockPicking { get; set; }
        [ProtoMember(2)] public double LockPickDamageChance { get; set; }
        [ProtoMember(3)] public float LockPickDamage { get; set; }
        [ProtoMember(4, IsRequired = true)] public bool RequiresPilferer { get; set; }
        [ProtoMember(5, IsRequired = true)] public bool RequiresTinkerer { get; set; }
        [ProtoMember(6)] public float StructureLockChance { get; set; }
        [ProtoMember(7)] public int StructureMinReinforcement { get; set; }
        [ProtoMember(8)] public int StructureMaxReinforcement { get; set; }
        [ProtoMember(9, IsRequired = true)] public bool ReinforcedBuildingBlocks { get; set; }
        [ProtoMember(10, IsRequired = true)] public bool ReinforceAllBlocks { get; set; }
        [ProtoMember(11)] public int BlackBronzePadlockDifficulty { get; set; }
        [ProtoMember(12)] public int BismuthBronzePadlockDifficulty { get; set; }
        [ProtoMember(13)] public int TinBronzePadlockDifficulty { get; set; }
        [ProtoMember(14)] public int IronPadlockDifficulty { get; set; }
        [ProtoMember(15)] public int MeteoricIronPadlockDifficulty { get; set; }
        [ProtoMember(16)] public int SteelPadlockDifficulty { get; set; }
        [ProtoMember(17)] public int CopperPadlockDifficulty { get; set; }
        [ProtoMember(18)] public int NickelPadlockDifficulty { get; set; }
        [ProtoMember(19)] public int SilverPadlockDifficulty { get; set; }
        [ProtoMember(20)] public int GoldPadlockDifficulty { get; set; }
        [ProtoMember(21)] public int TitaniumPadlockDifficulty { get; set; }
        [ProtoMember(22)] public int LeadPadlockDifficulty { get; set; }
        [ProtoMember(23)] public int ZincPadlockDifficulty { get; set; }
        [ProtoMember(24)] public int TinPadlockDifficulty { get; set; }
        [ProtoMember(25)] public int ChromiumPadlockDifficulty { get; set; }
        [ProtoMember(26)] public int CupronickelPadlockDifficulty { get; set; }
        [ProtoMember(27)] public int ElectrumPadlockDifficulty { get; set; }
        [ProtoMember(28)] public int PlatinumPadlockDifficulty { get; set; }
        [ProtoMember(29)] public float AgedKeyDamageChance { get; set; }
        [ProtoMember(30)] public int AgedKeyDamage { get; set; }
        [ProtoMember(31)] public double StructureKeyChance { get; set; }
        [ProtoMember(32)] public int LockToolDamage { get; set; }
        [ProtoMember(33, IsRequired = true)] public bool OwnerExempt { get; set; }
        [ProtoMember(34, IsRequired = true)] public bool LockpickingMinigame { get; set; }
        [ProtoMember(35)]
        public List<string> StructureBlacklist { get; set; }
        [ProtoMember(36)] public int MinigameBindingOrderThreshold { get; set; }

        private static List<string> GetDefaultBlacklist() => new List<string>
        {
            "tradercavaran:large-1.json",
            "tradercavaran:large-2.json",
            "tradercavaran:luxury-1.json",
            "tradercavaran:small-1.json",
            "tradercavaran:small-2.json",
            "tradercavaran:small-3.json",
            "tradercavaran:small-4.json",

            "tradercavaranwagon:large-1.json",
            "tradercavaranwagon:large-2.json",
            "tradercavaranwagon:large-3.json",
            "tradercavaranwagon:large-4.json",
            "tradercavaranwagon:large-5.json",
            "tradercavaranwagon:large-6.json",
            "tradercavaranwagon:luxury-1.json",
            "tradercavaranwagon:small-1.json",
            "tradercavaranwagon:small-2.json",
            "tradercavaranwagon:small-3.json",
            "tradercavaranwagon:small-4.json",
            "tradercavaranwagon:small-5.json",
            "tradercavaranwagon:small-6.json",
            "tradercavaranwagon:small-7.json",
            "tradercavaranwagon:small-8.json",
            "tradercavaranwagon:small-9.json",
            "tradercavaranwagon:small-10.json",
            "tradercavaranwagon:small-11.json",
            "tradercavaranwagon:small-12.json",
            "tradercavaranwagon:small-13.json",
            "tradercavaranwagon:small-14.json",
            "tradercavaranwagon:small-15.json",
            "tradercavaranwagon:small-16.json",

            "tradersettlement:large-1.json",
            "tradersettlement:large-2.json",
            "tradersettlement:medium-1.json",
            "tradersettlement:medium-2.json",
            "tradersettlement:medium-3.json",
            "tradersettlement:small-1.json",
            "tradersettlement:small-2.json",

            "tradersettlement:kitchenwarespawnhouse.json",
            "tradersettlement:booktraderschematic1",
            "tradercaravan:bellcross-trader1-single.json",
            "tradercaravan:traderwagons1.json",
            "tradercaravan:traderwagons2.json",
            "tradercaravan:traderwagons3.json",
            "tradercaravan:traderwagons4.json",
            "tradercaravan:traderwagonswithtent1-leather.json",
            "tradercaravan:traderwagonswithtent1-rock-andesite.json",
            "tradercaravan:traderwagonswithtent1-rock-basalt.json",
            "tradercaravan:traderwagonswithtent1-rock-bauxite.json",
            "tradercaravan:traderwagonswithtent1-rock-claystone.json",
            "tradercaravan:traderwagonswithtent1-rock-halite.json",
            "tradercaravan:traderwagonswithtent1-rock-peridotite.json",
            "tradercaravan:traderwagonswithtent1-rock-scoria.json",
            "tradercaravan:traderwagonswithtent1-rock-shale.json",
            "tradercaravan:traderwagonswithtent2-leather.json",
            "tradercaravan:traderwagonswithtent2-rock-andesite.json",
            "tradercaravan:traderwagonswithtent2-rock-basalt.json",
            "tradercaravan:traderwagonswithtent2-rock-bauxite.json",
            "tradercaravan:traderwagonswithtent2-rock-claystone.json",
            "tradercaravan:traderwagonswithtent2-rock-halite.json",
            "tradercaravan:traderwagonswithtent2-rock-peridotite.json",
            "tradercaravan:traderwagonswithtent2-rock-scoria.json",
            "tradercaravan:traderwagonswithtent2-rock-shale.json",
            "tradercaravan:traderwagonswithtent3-leather.json",
            "tradercaravan:traderwagonswithtent3-rock-andesite.json",
            "tradercaravan:traderwagonswithtent3-rock-basalt.json",
            "tradercaravan:traderwagonswithtent3-rock-bauxite.json",
            "tradercaravan:traderwagonswithtent3-rock-claystone.json",
            "tradercaravan:traderwagonswithtent3-rock-halite.json",
            "tradercaravan:traderwagonswithtent3-rock-peridotite.json",
            "tradercaravan:traderwagonswithtent3-rock-scoria.json",
            "tradercaravan:traderwagonswithtent3-rock-shale.json",
            "tradercaravan:traderwagonswithtent4-leather.json",
            "tradercaravan:traderwagonswithtent4-rock-andesite.json",
            "tradercaravan:traderwagonswithtent4-rock-basalt.json",
            "tradercaravan:traderwagonswithtent4-rock-bauxite.json",
            "tradercaravan:traderwagonswithtent4-rock-claystone.json",
            "tradercaravan:traderwagonswithtent4-rock-halite.json",
            "tradercaravan:traderwagonswithtent4-rock-peridotite.json",
            "tradercaravan:traderwagonswithtent4-rock-scoria.json",
            "tradercaravan:traderwagonswithtent4-rock-shale.json",
            "tradercaravan:traderwagonswithtent5-leather.json",
            "tradercaravan:traderwagonswithtent5-rock-andesite.json",
            "tradercaravan:traderwagonswithtent5-rock-basalt.json",
            "tradercaravan:traderwagonswithtent5-rock-bauxite.json",
            "tradercaravan:traderwagonswithtent5-rock-claystone.json",
            "tradercaravan:traderwagonswithtent5-rock-halite.json",
            "tradercaravan:traderwagonswithtent5-rock-peridotite.json",
            "tradercaravan:traderwagonswithtent5-rock-scoria.json",
            "tradercaravan:traderwagonswithtent5-rock-shale.json",
            "tradercaravan:traderwagonswithtent6-leather.json",
            "tradercaravan:traderwagonswithtent6-rock-andesite.json",
            "tradercaravan:traderwagonswithtent6-rock-basalt.json",
            "tradercaravan:traderwagonswithtent6-rock-bauxite.json",
            "tradercaravan:traderwagonswithtent6-rock-claystone.json",
            "tradercaravan:traderwagonswithtent6-rock-halite.json",
            "tradercaravan:traderwagonswithtent6-rock-peridotite.json",
            "tradercaravan:traderwagonswithtent6-rock-scoria.json",
            "tradercaravan:traderwagonswithtent6-rock-shale.json",
            "tradercaravan:traderwagonswithtent7-leather.json",
            "tradercaravan:traderwagonswithtent7-rock-andesite.json",
            "tradercaravan:traderwagonswithtent7-rock-basalt.json",
            "tradercaravan:traderwagonswithtent7-rock-bauxite.json",
            "tradercaravan:traderwagonswithtent7-rock-claystone.json",
            "tradercaravan:traderwagonswithtent7-rock-halite.json",
            "tradercaravan:traderwagonswithtent7-rock-peridotite.json",
            "tradercaravan:traderwagonswithtent7-rock-scoria.json",
            "tradercaravan:traderwagonswithtent7-rock-shale.json",
            "tradercaravan:traderwagonswithtent8-leather.json",
            "tradercaravan:traderwagonswithtent8-rock-andesite.json",
            "tradercaravan:traderwagonswithtent8-rock-basalt.json",
            "tradercaravan:traderwagonswithtent8-rock-bauxite.json",
            "tradercaravan:traderwagonswithtent8-rock-claystone.json",
            "tradercaravan:traderwagonswithtent8-rock-halite.json",
            "tradercaravan:traderwagonswithtent8-rock-peridotite.json",
            "tradercaravan:traderwagonswithtent8-rock-scoria.json",
            "tradercaravan:traderwagonswithtent8-rock-shale.json",
            "tradercaravan:bellcross-trader1-withfireplace.json",
            "tradercaravan:rarerustytrader-o1-1.json",
            "tradercaravan:rarerustytrader-o1-2.json",
            "tradercaravan:rarerustytrader-o1-3.json",
            "tradercaravan:rarerustytrader-o1-4.json",
            "tradercaravan:rarerustytrader1.json",
            "tradercaravan:rarerustytrader2.json",
            "tradercaravan:rarerustytrader3.json",
            "tradercaravan:rustytraderwithtent-1-leather.json",
            "tradercaravan:rustytraderwithtent-1-rock-andesite.json",
            "tradercaravan:rustytraderwithtent-1-rock-basalt.json",
            "tradercaravan:rustytraderwithtent-1-rock-bauxite.json",
            "tradercaravan:rustytraderwithtent-1-rock-claystone.json",
            "tradercaravan:rustytraderwithtent-1-rock-halite.json",
            "tradercaravan:rustytraderwithtent-1-rock-peridotite.json",
            "tradercaravan:rustytraderwithtent-1-rock-scoria.json",
            "tradercaravan:rustytraderwithtent-1-rock-shale.json",
            "tradercaravan:rustytraderwithtent-2-leather.json",
            "tradercaravan:rustytraderwithtent-2-rock-andesite.json",
            "tradercaravan:rustytraderwithtent-2-rock-basalt.json",
            "tradercaravan:rustytraderwithtent-2-rock-bauxite.json",
            "tradercaravan:rustytraderwithtent-2-rock-claystone.json",
            "tradercaravan:rustytraderwithtent-2-rock-halite.json",
            "tradercaravan:rustytraderwithtent-2-rock-peridotite.json",
            "tradercaravan:rustytraderwithtent-2-rock-scoria.json",
            "tradercaravan:rustytraderwithtent-2-rock-shale.json",
            "tradercaravan:rustytraderwithtent-3-leather.json",
            "tradercaravan:rustytraderwithtent-3-rock-andesite.json",
            "tradercaravan:rustytraderwithtent-3-rock-basalt.json",
            "tradercaravan:rustytraderwithtent-3-rock-bauxite.json",
            "tradercaravan:rustytraderwithtent-3-rock-claystone.json",
            "tradercaravan:rustytraderwithtent-3-rock-halite.json",
            "tradercaravan:rustytraderwithtent-3-rock-peridotite.json",
            "tradercaravan:rustytraderwithtent-3-rock-scoria.json",
            "tradercaravan:rustytraderwithtent-3-rock-shale.json",
            "tradercaravan:rustytraderwithtent-4-leather.json",
            "tradercaravan:rustytraderwithtent-4-rock-andesite.json",
            "tradercaravan:rustytraderwithtent-4-rock-basalt.json",
            "tradercaravan:rustytraderwithtent-4-rock-bauxite.json",
            "tradercaravan:rustytraderwithtent-4-rock-claystone.json",
            "tradercaravan:rustytraderwithtent-4-rock-halite.json",
            "tradercaravan:rustytraderwithtent-4-rock-peridotite.json",
            "tradercaravan:rustytraderwithtent-4-rock-scoria.json",
            "tradercaravan:rustytraderwithtent-4-rock-shale.json",
            "tradercaravan:rustytraderwithtent-5-leather.json",
            "tradercaravan:rustytraderwithtent-5-rock-andesite.json",
            "tradercaravan:rustytraderwithtent-5-rock-basalt.json",
            "tradercaravan:rustytraderwithtent-5-rock-bauxite.json",
            "tradercaravan:rustytraderwithtent-5-rock-claystone.json",
            "tradercaravan:rustytraderwithtent-5-rock-halite.json",
            "tradercaravan:rustytraderwithtent-5-rock-peridotite.json",
            "tradercaravan:rustytraderwithtent-5-rock-scoria.json",
            "tradercaravan:rustytraderwithtent-5-rock-shale.json",
            "tradercaravan:rustynottenttrader1.json",
            "tradercaravan:rustynottenttrader2.json",
            "tradercaravan:rustynottenttrader3.json",
            "tradercaravan:rustynottenttrader4.json",
            "tradercaravan:rustynottenttrader5.json",
            "tradercaravan:rustynottenttrader6.json",
            "tradercaravan:rustynottenttrader7.json",
            "tradercaravan:rustynottenttrader8.json",
            "tradercaravan:rarerustynottenttrader1.json",
            "tradercaravan:rarerustynottenttrader2.json",
            "tradercaravan:rarerustynottenttrader3.json",
            "tradercaravan:rarerustynottenttrader4.json",
            "tradercaravan:coldclimate-o2-stadrian-v1.json",
            "tradercaravan:coldclimate-o2-stadrian-v2.json",
            "tradercaravan:coldclimate-o2-stadrian-v3.json",
            "tradercaravan:coldclimate-o2-stadrian-v4.json",
            "tradercaravan:coldclimate-o2-stadrian-v5.json",
            "tradercaravan:tractor1.json",
            "tradercaravan:bricklayers1.json",
            "tradercaravan:bricklayers2.json",
            "tradercaravan:bricklayers3.json",
            "tradercaravan:bricklayers4.json",
            "tradercaravan:expandedfoods1.json"
        };
        public Config()
        {
            LockPicking = true;
            LockPickDamageChance = 0.1f;
            LockPickDamage = 200;
            RequiresPilferer = true;
            RequiresTinkerer = true;
            StructureLockChance = 0.3f;
            StructureMinReinforcement = 50;
            StructureMaxReinforcement = 100;
            ReinforcedBuildingBlocks = false;
            ReinforceAllBlocks = false;
            BlackBronzePadlockDifficulty = 25;
            BismuthBronzePadlockDifficulty = 30;
            TinBronzePadlockDifficulty = 35;
            IronPadlockDifficulty = 60;
            MeteoricIronPadlockDifficulty = 70;
            SteelPadlockDifficulty = 80;
            CopperPadlockDifficulty = 10;
            NickelPadlockDifficulty = 15;
            LeadPadlockDifficulty = 20;
            TinPadlockDifficulty = 20;
            ZincPadlockDifficulty = 25;
            CupronickelPadlockDifficulty = 40;
            ChromiumPadlockDifficulty = 60;
            TitaniumPadlockDifficulty = 90;
            SilverPadlockDifficulty = 35;
            ElectrumPadlockDifficulty = 20;
            GoldPadlockDifficulty = 20;
            PlatinumPadlockDifficulty = 50;
            AgedKeyDamageChance = 0.75f;
            AgedKeyDamage = 20;
            StructureKeyChance = 0.03;
            LockToolDamage = 50;
            OwnerExempt = false;
            LockpickingMinigame = true;
            StructureBlacklist = GetDefaultBlacklist();
            MinigameBindingOrderThreshold = 75;
        }

        public Config(ICoreAPI api, Config previousConfig = null)
        {
            LockPicking = previousConfig?.LockPicking ?? true;
            LockPickDamageChance = previousConfig?.LockPickDamageChance ?? 0.1;
            LockPickDamage = previousConfig?.LockPickDamage ?? 200;
            RequiresPilferer = previousConfig?.RequiresPilferer ?? true;
            RequiresTinkerer = previousConfig?.RequiresTinkerer ?? true;
            StructureLockChance = previousConfig?.StructureLockChance ?? 0.3f;
            StructureMinReinforcement = previousConfig?.StructureMinReinforcement ?? 50;
            StructureMaxReinforcement = previousConfig?.StructureMaxReinforcement ?? 100;
            ReinforcedBuildingBlocks = previousConfig?.ReinforcedBuildingBlocks ?? false;
            ReinforceAllBlocks = previousConfig?.ReinforceAllBlocks ?? false;
            BlackBronzePadlockDifficulty = previousConfig?.BlackBronzePadlockDifficulty ?? 25;
            BismuthBronzePadlockDifficulty = previousConfig?.BismuthBronzePadlockDifficulty ?? 30;
            TinBronzePadlockDifficulty = previousConfig?.TinBronzePadlockDifficulty ?? 35;
            IronPadlockDifficulty = previousConfig?.IronPadlockDifficulty ?? 60;
            MeteoricIronPadlockDifficulty = previousConfig?.MeteoricIronPadlockDifficulty ?? 70;
            SteelPadlockDifficulty = previousConfig?.SteelPadlockDifficulty ?? 80;
            CopperPadlockDifficulty = previousConfig?.CopperPadlockDifficulty ?? 10;
            NickelPadlockDifficulty = previousConfig?.NickelPadlockDifficulty ?? 15;
            LeadPadlockDifficulty = previousConfig?.LeadPadlockDifficulty ?? 20;
            TinPadlockDifficulty = previousConfig?.TinPadlockDifficulty ?? 20;
            ZincPadlockDifficulty = previousConfig?.ZincPadlockDifficulty ?? 25;
            CupronickelPadlockDifficulty = previousConfig?.CupronickelPadlockDifficulty ?? 40;
            ChromiumPadlockDifficulty = previousConfig?.ChromiumPadlockDifficulty ?? 60;
            TitaniumPadlockDifficulty = previousConfig?.TitaniumPadlockDifficulty ?? 90;
            SilverPadlockDifficulty = previousConfig?.SilverPadlockDifficulty ?? 35;
            ElectrumPadlockDifficulty = previousConfig?.ElectrumPadlockDifficulty ?? 20;
            GoldPadlockDifficulty = previousConfig?.GoldPadlockDifficulty ?? 20;
            PlatinumPadlockDifficulty = previousConfig?.PlatinumPadlockDifficulty ?? 50;
            AgedKeyDamageChance = previousConfig?.AgedKeyDamageChance ?? 0.75f;
            AgedKeyDamage = previousConfig?.AgedKeyDamage ?? 20;
            StructureKeyChance = previousConfig?.StructureKeyChance ?? 0.03;
            LockToolDamage = previousConfig?.LockToolDamage ?? 50;
            OwnerExempt = previousConfig?.OwnerExempt ?? false;
            LockpickingMinigame = previousConfig?.LockpickingMinigame ?? false;
            StructureBlacklist = previousConfig?.StructureBlacklist ?? GetDefaultBlacklist();
            MinigameBindingOrderThreshold = previousConfig?.MinigameBindingOrderThreshold ?? 75;
        }
    }
}
