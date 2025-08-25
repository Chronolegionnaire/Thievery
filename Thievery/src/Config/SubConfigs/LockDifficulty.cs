using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Thievery.Config.SubConfigs;

public class LockDifficultyConfig
    {
        /// <summary>Difficulty (1–100) for Copper padlocks.</summary>
        [Category("Base Metals")]
        [Range(1, 100)]
        [DefaultValue(10)]
        public int CopperPadlockDifficulty { get; set; } = 10;

        /// <summary>Difficulty (1–100) for Nickel padlocks.</summary>
        [Category("Base Metals")]
        [Range(1, 100)]
        [DefaultValue(15)]
        public int NickelPadlockDifficulty { get; set; } = 15;

        /// <summary>Difficulty (1–100) for Lead padlocks.</summary>
        [Category("Base Metals")]
        [Range(1, 100)]
        [DefaultValue(20)]
        public int LeadPadlockDifficulty { get; set; } = 20;

        /// <summary>Difficulty (1–100) for Tin padlocks.</summary>
        [Category("Base Metals")]
        [Range(1, 100)]
        [DefaultValue(25)]
        public int TinPadlockDifficulty { get; set; } = 25;

        /// <summary>Difficulty (1–100) for Zinc padlocks.</summary>
        [Category("Base Metals")]
        [Range(1, 100)]
        [DefaultValue(30)]
        public int ZincPadlockDifficulty { get; set; } = 30;

        /// <summary>Difficulty (1–100) for Cupronickel padlocks.</summary>
        [Category("Alloys")]
        [Range(1, 100)]
        [DefaultValue(50)]
        public int CupronickelPadlockDifficulty { get; set; } = 50;

        /// <summary>Difficulty (1–100) for Tin Bronze padlocks.</summary>
        [Category("Bronzes")]
        [Range(1, 100)]
        [DefaultValue(35)]
        public int TinBronzePadlockDifficulty { get; set; } = 35;

        /// <summary>Difficulty (1–100) for Bismuth Bronze padlocks.</summary>
        [Category("Bronzes")]
        [Range(1, 100)]
        [DefaultValue(40)]
        public int BismuthBronzePadlockDifficulty { get; set; } = 40;

        /// <summary>Difficulty (1–100) for Black Bronze padlocks.</summary>
        [Category("Bronzes")]
        [Range(1, 100)]
        [DefaultValue(45)]
        public int BlackBronzePadlockDifficulty { get; set; } = 45;

        /// <summary>Difficulty (1–100) for Iron padlocks.</summary>
        [Category("Irons & Steels")]
        [Range(1, 100)]
        [DefaultValue(55)]
        public int IronPadlockDifficulty { get; set; } = 55;

        /// <summary>Difficulty (1–100) for Meteoric Iron padlocks.</summary>
        [Category("Irons & Steels")]
        [Range(1, 100)]
        [DefaultValue(60)]
        public int MeteoricIronPadlockDifficulty { get; set; } = 60;

        /// <summary>Difficulty (1–100) for Steel padlocks.</summary>
        [Category("Irons & Steels")]
        [Range(1, 100)]
        [DefaultValue(65)]
        public int SteelPadlockDifficulty { get; set; } = 65;

        /// <summary>Difficulty (1–100) for Silver padlocks.</summary>
        [Category("Precious Metals")]
        [Range(1, 100)]
        [DefaultValue(70)]
        public int SilverPadlockDifficulty { get; set; } = 70;

        /// <summary>Difficulty (1–100) for Electrum padlocks.</summary>
        [Category("Precious Metals")]
        [Range(1, 100)]
        [DefaultValue(75)]
        public int ElectrumPadlockDifficulty { get; set; } = 75;

        /// <summary>Difficulty (1–100) for Gold padlocks.</summary>
        [Category("Precious Metals")]
        [Range(1, 100)]
        [DefaultValue(80)]
        public int GoldPadlockDifficulty { get; set; } = 80;

        /// <summary>Difficulty (1–100) for Platinum padlocks.</summary>
        [Category("Precious Metals")]
        [Range(1, 100)]
        [DefaultValue(85)]
        public int PlatinumPadlockDifficulty { get; set; } = 85;

        /// <summary>Difficulty (1–100) for Chromium padlocks.</summary>
        [Category("Advanced")]
        [Range(1, 100)]
        [DefaultValue(90)]
        public int ChromiumPadlockDifficulty { get; set; } = 90;

        /// <summary>Difficulty (1–100) for Titanium padlocks.</summary>
        [Category("Advanced")]
        [Range(1, 100)]
        [DefaultValue(95)]
        public int TitaniumPadlockDifficulty { get; set; } = 95;
    }