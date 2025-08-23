using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Thievery.Config.SubConfigs;

public class WorldGenLockingAndReinforcementConfig
{
    /// <summary>Chance (0–1) that eligible worldgen structures spawn with a lock.</summary>
    [Category("Worldgen")]
    [DisplayFormat(DataFormatString = "P")]
    [Range(0d, 1d)]
    [DefaultValue(0.30d)]
    public double StructureLockChance { get; set; } = 0.60d;

    /// <summary>Minimum reinforcement value applied to structure blocks.</summary>
    [Category("Reinforcement")]
    [Range(0, int.MaxValue)]
    [DefaultValue(50)]
    public int StructureMinReinforcement { get; set; } = 50;

    /// <summary>Maximum reinforcement value applied to structure blocks.</summary>
    [Category("Reinforcement")]
    [Range(0, int.MaxValue)]
    [DefaultValue(100)]
    public int StructureMaxReinforcement { get; set; } = 100;

    /// <summary>If true, certain building blocks are reinforced by default.</summary>
    [Category("Reinforcement")]
    [DefaultValue(true)]
    public bool ReinforcedBuildingBlocks { get; set; } = true;

    /// <summary>If true, all blocks in a structure are reinforced.</summary>
    [Category("Reinforcement")]
    [DefaultValue(true)]
    public bool ReinforceAllBlocks { get; set; } = true;

    /// <summary>Chance (0–1) for a structure to generate a corresponding key.</summary>
    [Category("Keys")]
    [DisplayFormat(DataFormatString = "P")]
    [Range(0d, 1d)]
    [DefaultValue(0.03d)]
    public double StructureKeyChance { get; set; } = 0.03d;
}