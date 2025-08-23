using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Thievery.Config.SubConfigs;

public class LootPoolEntry
{
    [Required] public string Code { get; set; } = "game:flaxfibers";
    [Range(0, int.MaxValue)] public int Min { get; set; } = 1;
    [Range(0, int.MaxValue)] public int Max { get; set; } = 1;
    [Range(0, int.MaxValue)] public int Weight { get; set; } = 1;
}

public class TierLootConfig
{
    /// <summary>How many picks from the tier’s pool per reward grant (maximum).</summary>
    [DefaultValue(5)] public int Rolls { get; set; } = 5;

    /// <summary>
    /// Additional weight that represents "no item" on a roll.
    /// If 0, behavior matches old code (every roll yields an item, barring errors).
    /// </summary>
    [Range(0, int.MaxValue)] public int EmptyWeight { get; set; } = 0;

    /// <summary>Weighted entries; allow wildcards in Code.</summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<LootPoolEntry> Pool { get; set; } = new();

    /// <summary>Rusty gears drop range for non-container blocks.</summary>
    [Range(0, int.MaxValue)] public int GearsMin { get; set; } = 1;
    [Range(0, int.MaxValue)] public int GearsMax { get; set; } = 3;
}


public class LockpickRewardsConfig
{
    /// <summary>Master switch. If false, no items are granted and no gears drop.</summary>
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    [Category("Tier: Easy")]
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public TierLootConfig Easy { get; set; } = new();

    [Category("Tier: Medium")]
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public TierLootConfig Medium { get; set; } = new();

    [Category("Tier: Hard")]
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public TierLootConfig Hard { get; set; } = new();

    [Category("Tier: Brutal")]
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public TierLootConfig Brutal { get; set; } = new();
}

public static class LockpickDefaults
{
    public static LockpickRewardsConfig Create()
    {
        return new LockpickRewardsConfig
        {
            Enabled = true,

            Easy = new TierLootConfig
            {
                Rolls = 5,
                EmptyWeight = 5,
                Pool = new List<LootPoolEntry>
                {
                    new() { Code = "game:stackrandomizer-seed",                Min = 1, Max = 1, Weight = 3 },
                    new() { Code = "game:stackrandomizer-kitchen",             Min = 1, Max = 1, Weight = 3 },
                    new() { Code = "game:stackrandomizer-fuel",                Min = 1, Max = 1, Weight = 2 },
                    new() { Code = "game:stackrandomizer-resource",            Min = 1, Max = 1, Weight = 2 },
                    new() { Code = "game:stackrandomizer-cloth-lowstatus",     Min = 1, Max = 1, Weight = 1 },
                    new() { Code = "game:stackrandomizer-accessory-lowstatus", Min = 1, Max = 1, Weight = 1 }
                },
                GearsMin = 1, GearsMax = 3
            },

            Medium = new TierLootConfig
            {
                Rolls = 5,
                EmptyWeight = 5,
                Pool = new List<LootPoolEntry>
                {
                    new() { Code = "game:stackrandomizer-ore",                    Min = 1, Max = 1, Weight = 3 },
                    new() { Code = "game:stackrandomizer-kitchen",                Min = 1, Max = 1, Weight = 3 },
                    new() { Code = "game:stackrandomizer-coppertool",             Min = 1, Max = 1, Weight = 1 },
                    new() { Code = "game:stackrandomizer-copperweapon",           Min = 1, Max = 1, Weight = 1 },
                    new() { Code = "game:stackrandomizer-cloth-mediumstatus",     Min = 1, Max = 1, Weight = 1 },
                    new() { Code = "game:stackrandomizer-accessory-mediumstatus", Min = 1, Max = 1, Weight = 1 }
                },
                GearsMin = 2, GearsMax = 5
            },

            Hard = new TierLootConfig
            {
                Rolls = 5,
                EmptyWeight = 5,
                Pool = new List<LootPoolEntry>
                {
                    new() { Code = "game:stackrandomizer-ruinedweapon",       Min = 1, Max = 1, Weight = 3 },
                    new() { Code = "game:stackrandomizer-armor",              Min = 1, Max = 1, Weight = 2 },
                    new() { Code = "game:stackrandomizer-cloth-highstatus",   Min = 1, Max = 1, Weight = 1 },
                    new() { Code = "game:stackrandomizer-accessory-highstatus",Min = 1, Max = 1, Weight = 1 },
                    new() { Code = "game:stackrandomizer-ingot",              Min = 1, Max = 2, Weight = 3 }
                },
                GearsMin = 4, GearsMax = 8
            },

            Brutal = new TierLootConfig
            {
                Rolls = 5,
                EmptyWeight = 5,
                Pool = new List<LootPoolEntry>
                {
                    new() { Code = "game:stackrandomizer-alljonas",        Min = 1, Max = 1, Weight = 1 },
                    new() { Code = "game:stackrandomizer-lantern",         Min = 1, Max = 1, Weight = 1 },
                    new() { Code = "game:stackrandomizer-tuningcylinder",  Min = 1, Max = 1, Weight = 1 },
                    new() { Code = "game:stackrandomizer-cloth-highstatus",Min = 1, Max = 1, Weight = 2 },
                    new() { Code = "game:stackrandomizer-accessory-highstatus", Min = 1, Max = 1, Weight = 2 },
                    new() { Code = "game:stackrandomizer-armor",           Min = 1, Max = 1, Weight = 1 },
                    new() { Code = "game:stackrandomizer-painting",        Min = 1, Max = 1, Weight = 2 }
                },
                GearsMin = 6, GearsMax = 12
            }
        };
    }
}