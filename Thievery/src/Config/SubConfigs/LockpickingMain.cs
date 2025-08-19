using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Thievery.Config.SubConfigs;

public class LockpickingMainConfig
    {
        /// <summary>Master toggle for the lockpicking feature.</summary>
        [Category("Main")]
        [DefaultValue(true)]
        public bool LockPicking { get; set; } = true;

        /// <summary>Chance (0–1) that a lockpick takes durability damage on use.</summary>
        [Category("Main")]
        [DisplayFormat(DataFormatString = "P")]
        [Range(0d, 1d)]
        [DefaultValue(0.10d)]
        public double LockPickDamageChance { get; set; } = 0.10d;

        /// <summary>Damage applied to a lockpick when damage is rolled.</summary>
        [Category("Main")]
        [Range(0d, double.PositiveInfinity)]
        [DefaultValue(200d)]
        public float LockPickDamage { get; set; } = 200f;

        /// <summary>If true, player must have the Pilferer attribute to pick locks.</summary>
        [Category("Requirements")]
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<string> RequiredTraits  { get; set; } = new()
        {
            "pilferer",
            "tinkerer"
        };

        /// <summary>Chance (0–1) an aged key is damaged on use.</summary>
        [Category("Aged Key")]
        [DisplayFormat(DataFormatString = "P")]
        [Range(0d, 1d)]
        [DefaultValue(0.75d)]
        public double AgedKeyDamageChance { get; set; } = 0.75d;

        /// <summary>Durability damage to an aged key when damage is rolled.</summary>
        [Category("Aged Key")]
        [Range(0, int.MaxValue)]
        [DefaultValue(20)]
        public int AgedKeyDamage { get; set; } = 20;

        /// <summary>Durability damage applied to generic lock tools.</summary>
        [Category("Tools")]
        [Range(0, int.MaxValue)]
        [DefaultValue(50)]
        public int LockToolDamage { get; set; } = 50;

        /// <summary>If true, owners are exempt from having to unlock blocks to use them.</summary>
        [Category("Permissions")]
        [DefaultValue(false)]
        public bool OwnerExempt { get; set; } = false;
        
        [Category("Permissions")]
        [DefaultValue(false)]
        public bool BlockLockpickOnLandClaims { get; set; } = false;
    }