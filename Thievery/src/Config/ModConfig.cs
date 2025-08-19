using Thievery.Config.SubConfigs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;


namespace Thievery.Config;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class ModConfig
{
    public const string ConfigPath = "ThieveryConfig.json";

    public static ModConfig Instance { get; internal set; }

    /// <summary>
    /// The configuration for thirst mechanics
    /// </summary>
    public LockpickingMainConfig Main { get; set; } = new();

    /// <summary>
    /// The configuration for satiety mechanics
    /// </summary>
    public LockDifficultyConfig Difficulty { get; set; } = new();

    /// <summary>
    /// The configuration for the perish rates
    /// </summary>
    public LockpickingMiniGameConfig MiniGame { get; set; } = new();

    /// <summary>
    /// The configuration for heat and cooling mechanics
    /// </summary>
    public WorldGenLockingAndReinforcementConfig WorldGen { get; set; } = new();
    
    /// <summary>
    /// The configuration for liquid encumbrance mechanics
    /// </summary>
    public StructureBlacklistConfig Blacklist { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JToken> LegacyData { get; set; }
}
