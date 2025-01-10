using ProtoBuf;
using Vintagestory.API.Common;

namespace Thievery.Config;

[ProtoContract]
public class Config : IModConfig
{
    [ProtoMember(1)] public bool LockPicking { get; set; }
    [ProtoMember(2)] public float BlackBronzePadlockPickDurationSeconds { get; set; }
    [ProtoMember(3)] public float BismuthBronzePadlockPickDurationSeconds { get; set; }
    [ProtoMember(4)] public float TinBronzePadlockPickDurationSeconds { get; set; }
    [ProtoMember(5)] public float IronPadlockPickDurationSeconds { get; set; }
    [ProtoMember(6)] public float MeteoricIronPadlockPickDurationSeconds { get; set; }
    [ProtoMember(7)] public float SteelPadlockPickDurationSeconds { get; set; }
    [ProtoMember(8)] public double LockPickDamageChance { get; set; }
    [ProtoMember(9)] public float LockPickDamage { get; set; }
    [ProtoMember(10)] public bool RequiresPilferer { get; set; }
    [ProtoMember(11)] public bool RequiresTinkerer { get; set; }
    public Config()
    {
    }

    public Config(ICoreAPI api, Config previousConfig = null)
    {
        LockPicking = previousConfig?.LockPicking ?? true;
        BlackBronzePadlockPickDurationSeconds = previousConfig?.BlackBronzePadlockPickDurationSeconds ?? 20;
        BismuthBronzePadlockPickDurationSeconds = previousConfig?.BismuthBronzePadlockPickDurationSeconds ?? 24;
        TinBronzePadlockPickDurationSeconds = previousConfig?.TinBronzePadlockPickDurationSeconds ?? 28;
        IronPadlockPickDurationSeconds = previousConfig?.IronPadlockPickDurationSeconds ?? 60;
        MeteoricIronPadlockPickDurationSeconds = previousConfig?.MeteoricIronPadlockPickDurationSeconds ?? 100;
        SteelPadlockPickDurationSeconds = previousConfig?.SteelPadlockPickDurationSeconds ?? 180;
        LockPickDamageChance = previousConfig?.LockPickDamageChance ?? 0.1;
        LockPickDamage = previousConfig?.LockPickDamage ?? 200;
        RequiresPilferer = previousConfig?.RequiresPilferer ?? true;
        RequiresTinkerer = previousConfig?.RequiresTinkerer ?? true;
    }
}