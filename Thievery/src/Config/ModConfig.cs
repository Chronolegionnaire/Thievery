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
    /// Main mod config
    /// </summary>
    public LockpickingMainConfig Main { get; set; } = new();
    
    /// <summary>
    /// Pickpocket config
    /// </summary>
    public PickpocketingMainConfig Pickpocket { get; set; } = new();

    /// <summary>
    /// Lock difficulty assignment
    /// </summary>
    public LockDifficultyConfig Difficulty { get; set; } = new();

    /// <summary>
    /// Mini game config
    /// </summary>
    public LockpickingMiniGameConfig MiniGame { get; set; } = new();
    
    /// <summary>
    /// Minigame loot options
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public LockpickRewardsConfig Rewards { get; set; }

    /// <summary>
    /// World gen config
    /// </summary>
    public WorldGenLockingAndReinforcementConfig WorldGen { get; set; } = new();
    
    /// <summary>
    /// World gen structure blacklist
    /// </summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public StructureBlacklistConfig Blacklist { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JToken> LegacyData { get; set; }
}
