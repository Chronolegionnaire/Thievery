using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Thievery.Config.SubConfigs;

public class LockpickingMiniGameConfig
{
    /// <summary>Enables the interactive lockpicking minigame.</summary>
    [Category("Minigame")]
    [DefaultValue(true)]
    public bool LockpickingMinigame { get; set; } = true;
    
    /// <summary>Base durability damage to the lockpick per failed/minigame tick.</summary>
    [Category("Damage")]
    [Range(0, int.MaxValue)]
    [DefaultValue(100)]
    public int LockpickDamageBase { get; set; } = 75;

    /// <summary>Base durability damage to the tension wrench per failed/minigame tick.</summary>
    [Category("Damage")]
    [Range(0, int.MaxValue)]
    [DefaultValue(20)]
    public int TensionWrenchDamageBase { get; set; } = 10;

    /// <summary>Binding order threshold (0–100) for pin feedback/ordering.</summary>
    [Category("Tuning")]
    [Range(0, 100)]
    [DefaultValue(75)]
    public int BindingOrderThreshold { get; set; } = 75;
    
    [Category("Tuning")]
    [Range(0, 0.5)]
    [DefaultValue(0.15)]
    public float HotspotForgivenessBins { get; set; } = 0.15f;
}