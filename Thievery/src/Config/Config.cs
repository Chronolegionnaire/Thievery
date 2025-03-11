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
        [ProtoMember(11)] public float BlackBronzePadlockPickDurationSeconds { get; set; }
        [ProtoMember(12)] public float BismuthBronzePadlockPickDurationSeconds { get; set; }
        [ProtoMember(13)] public float TinBronzePadlockPickDurationSeconds { get; set; }
        [ProtoMember(14)] public float IronPadlockPickDurationSeconds { get; set; }
        [ProtoMember(15)] public float MeteoricIronPadlockPickDurationSeconds { get; set; }
        [ProtoMember(16)] public float SteelPadlockPickDurationSeconds { get; set; }
        [ProtoMember(17)] public float CopperPadlockPickDurationSeconds { get; set; }
        [ProtoMember(18)] public float NickelPadlockPickDurationSeconds { get; set; }
        [ProtoMember(19)] public float SilverPadlockPickDurationSeconds { get; set; }
        [ProtoMember(20)] public float GoldPadlockPickDurationSeconds { get; set; }
        [ProtoMember(21)] public float TitaniumPadlockPickDurationSeconds { get; set; }
        [ProtoMember(22)] public float LeadPadlockPickDurationSeconds { get; set; }
        [ProtoMember(23)] public float ZincPadlockPickDurationSeconds { get; set; }
        [ProtoMember(24)] public float TinPadlockPickDurationSeconds { get; set; }
        [ProtoMember(25)] public float ChromiumPadlockPickDurationSeconds { get; set; }
        [ProtoMember(26)] public float CupronickelPadlockPickDurationSeconds { get; set; }
        [ProtoMember(27)] public float ElectrumPadlockPickDurationSeconds { get; set; }
        [ProtoMember(28)] public float PlatinumPadlockPickDurationSeconds { get; set; }
        [ProtoMember(29)] public float AgedKeyDamageChance { get; set; }
        [ProtoMember(30)] public int AgedKeyDamage { get; set; }
        [ProtoMember(31)] public double StructureKeyChance { get; set; }
        [ProtoMember(32)] public int LockToolDamage { get; set; }
        [ProtoMember(33, IsRequired = true)] public bool OwnerExempt { get; set; }


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
            BlackBronzePadlockPickDurationSeconds = 20;
            BismuthBronzePadlockPickDurationSeconds = 24;
            TinBronzePadlockPickDurationSeconds = 28;
            IronPadlockPickDurationSeconds = 60;
            MeteoricIronPadlockPickDurationSeconds = 100;
            SteelPadlockPickDurationSeconds = 180;
            CopperPadlockPickDurationSeconds = 10;
            NickelPadlockPickDurationSeconds = 30;
            LeadPadlockPickDurationSeconds = 15;
            TinPadlockPickDurationSeconds = 18;
            ZincPadlockPickDurationSeconds = 30;
            CupronickelPadlockPickDurationSeconds = 50;
            ChromiumPadlockPickDurationSeconds = 150;
            TitaniumPadlockPickDurationSeconds = 210;
            SilverPadlockPickDurationSeconds = 240;
            ElectrumPadlockPickDurationSeconds = 270;
            GoldPadlockPickDurationSeconds = 300;
            PlatinumPadlockPickDurationSeconds = 360;
            AgedKeyDamageChance = 0.75f;
            AgedKeyDamage = 20;
            StructureKeyChance = 0.03;
            LockToolDamage = 50;
            OwnerExempt = false;
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
            BlackBronzePadlockPickDurationSeconds = previousConfig?.BlackBronzePadlockPickDurationSeconds ?? 20;
            BismuthBronzePadlockPickDurationSeconds = previousConfig?.BismuthBronzePadlockPickDurationSeconds ?? 24;
            TinBronzePadlockPickDurationSeconds = previousConfig?.TinBronzePadlockPickDurationSeconds ?? 28;
            IronPadlockPickDurationSeconds = previousConfig?.IronPadlockPickDurationSeconds ?? 60;
            MeteoricIronPadlockPickDurationSeconds = previousConfig?.MeteoricIronPadlockPickDurationSeconds ?? 100;
            SteelPadlockPickDurationSeconds = previousConfig?.SteelPadlockPickDurationSeconds ?? 180;
            CopperPadlockPickDurationSeconds = previousConfig?.CopperPadlockPickDurationSeconds ?? 10;
            NickelPadlockPickDurationSeconds = previousConfig?.NickelPadlockPickDurationSeconds ?? 30;
            LeadPadlockPickDurationSeconds = previousConfig?.LeadPadlockPickDurationSeconds ?? 15;
            TinPadlockPickDurationSeconds = previousConfig?.TinPadlockPickDurationSeconds ?? 18;
            ZincPadlockPickDurationSeconds = previousConfig?.ZincPadlockPickDurationSeconds ?? 30;
            CupronickelPadlockPickDurationSeconds = previousConfig?.CupronickelPadlockPickDurationSeconds ?? 50;
            ChromiumPadlockPickDurationSeconds = previousConfig?.ChromiumPadlockPickDurationSeconds ?? 150;
            TitaniumPadlockPickDurationSeconds = previousConfig?.TitaniumPadlockPickDurationSeconds ?? 210;
            SilverPadlockPickDurationSeconds = previousConfig?.SilverPadlockPickDurationSeconds ?? 240;
            ElectrumPadlockPickDurationSeconds = previousConfig?.ElectrumPadlockPickDurationSeconds ?? 270;
            GoldPadlockPickDurationSeconds = previousConfig?.GoldPadlockPickDurationSeconds ?? 300;
            PlatinumPadlockPickDurationSeconds = previousConfig?.PlatinumPadlockPickDurationSeconds ?? 360;
            AgedKeyDamageChance = previousConfig?.AgedKeyDamageChance ?? 0.75f;
            AgedKeyDamage = previousConfig?.AgedKeyDamage ?? 20;
            StructureKeyChance = previousConfig?.StructureKeyChance ?? 0.03;
            LockToolDamage = previousConfig?.LockToolDamage ?? 50;
            OwnerExempt = previousConfig?.OwnerExempt ?? false;
        }
    }
}
