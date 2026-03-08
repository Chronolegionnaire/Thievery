using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Thievery.Config.SubConfigs;

public class PickpocketingMainConfig
    {
        /// <summary>Master toggle for the pick pocketing feature.</summary>
        [Category("Main")]
        [DefaultValue(true)]
        public bool PickPocketing { get; set; } = true;
        
        [Category("Main")]
        [DefaultValue(false)]
        public bool stealFullStack { get; set; } = false;

        /// <summary>List of traits that grant the ability to pick pockets.</summary>
        [Category("Requirements")]
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<string> RequiredTraits  { get; set; } = new()
        {
            "pilferer"
        };
        
        /// <summary>Chance (0–1) that a lockpick takes durability damage on use.</summary>
        [Category("Main")]
        [DisplayFormat(DataFormatString = "P")]
        [Range(0d, 1d)]
        [DefaultValue(0.35d)]
        public double baseSuccessChance { get; set; } = 0.35d;
        
        /// <summary>Chance (0–1) that a lockpick takes durability damage on use.</summary>
        [Category("Main")]
        [DisplayFormat(DataFormatString = "P")]
        [Range(0d, 1d)]
        [DefaultValue(0.35d)]
        public double alertChance { get; set; } = 0.35d;
        
        [Category("Timing")]
        [Range(0.1d, 30d)]                 // sanity clamp (0.1s .. 30s)
        [DefaultValue(1.6d)]
        public double PickpocketSeconds { get; set; } = 1.6d;
        
    }