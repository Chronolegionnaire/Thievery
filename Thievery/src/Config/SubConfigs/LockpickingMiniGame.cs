using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Thievery.Config.SubConfigs
{
    public class LockpickingMiniGameConfig
    {
        /// <summary>Enables the interactive lockpicking minigame.</summary>
        [Category("Minigame")]
        [DefaultValue(true)]
        public bool LockpickingMinigame { get; set; } = true;
        
        /// <summary>Base durability damage to the lockpick per click.</summary>
        [Category("Damage")]
        [Range(0, int.MaxValue)]
        [DefaultValue(1.5)]
        public double LockpickDamageBase { get; set; } = 1.5;

        /// <summary>Base durability damage (before difficulty multiplier) to the tension wrench per minigame tick.</summary>
        [Category("Damage")]
        [Range(0, int.MaxValue)]
        [DefaultValue(2)]
        public double TensionWrenchDamageBase { get; set; } = 0.5;
        
        
        /// <summary>Initial durability damage multiplier to the tension wrench on mini game start. 0 to disable.</summary>
        [Category("Damage")]
        [Range(0, int.MaxValue)]
        [DefaultValue(3)]
        public int InitialDamageMultiplier { get; set; } = 3;

        /// <summary>
        /// Binding order threshold (0–100) for pin feedback/ordering.
        /// Lock levels above this level will have a specific sequence they will need to be picked in
        /// </summary>
        [Category("Tuning")]
        [Range(0, 100)]
        [DefaultValue(75)]
        public int BindingOrderThreshold { get; set; } = 75;
        
        /// <summary>
        /// Additional forgiveness area size for successfully hitting a hotspot
        /// </summary>
        [Category("Tuning")]
        [Range(0, 0.5)]
        [DefaultValue(0.15)]
        public float HotspotForgivenessBins { get; set; } = 0.15f;

        /// <summary>How many minutes a lock is unpickable after it breaks. This only applys to the person who broke the lock
        /// so as to avoid exploitation.
        /// </summary>
        [Category("Probe")]
        [Range(1, 120)]
        [DefaultValue(10)]
        public int ProbeLockoutMinutes { get; set; } = 10;

        /// <summary>
        /// Per-press increment to the cumulative break chance after the free probes are used.
        /// Example: 0.18 → 18% added each additional probe after the free ones.
        /// </summary>
        [Category("Probe")]
        [Range(0.0, 1.0)]
        [DefaultValue(0.05)]
        public double ProbeBreakChanceIncrement { get; set; } = 0.05;

        /// <summary>Number of probe presses that are free of break risk. Set to -1 to have infinite free probes.</summary>
        [Category("Probe")]
        [Range(0, 10)]
        [DefaultValue(6)]
        public int ProbeFreeUses { get; set; } = 6;
        
        /// <summary>Whether broken locks are permanent.
        ///This only applys to the person who broke the lock so as to avoid exploitation.
        /// </summary>
        [Category("Probe")]
        [DefaultValue(false)]
        public bool PermanentLockBreak { get; set; } = false;
    }
}
