using System;
using System.Collections.Generic;
using Thievery.Config;
using Thievery.LockAndKey;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Thievery.LockAndKey.BreachingCharge
{
    public class BlockEntityBreachingCharge : BlockEntity
    {
        public float RemainingSeconds;
        private bool lit;
        private string ignitedByPlayerUid;
        private ILoadedSound fuseSound;
        public bool CascadeLit { get; private set; }
        private float scanRadius;
        private (int min, int max) reinforcementDamageRoll;
        private bool unlockLocks;
        private float containerDamageChance;
        private bool requireClaimPermission;
        private float knockbackStrength;

        public bool IsLit => lit;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            var at = Block?.Attributes;
            scanRadius = at?["blastRadiusByType"].AsInt(3) ?? at?["blastRadius"].AsInt(3) ?? 3;
            reinforcementDamageRoll = ParseRange(at?["reinforcementDamageRollByType"].AsString("1-3"));
            unlockLocks = at?["unlockLocks"].AsBool(true) ?? true;
            containerDamageChance = (float)(at?["containerDamageChance"].AsDouble(0.15) ?? 0.15);
            requireClaimPermission = at?["requireClaimPermission"].AsBool(true) ?? true;
            knockbackStrength = (float)(at?["knockbackStrength"].AsDouble(1.0) ?? 1.0);
            RegisterGameTickListener(OnTick, 50);

            if (api.Side == EnumAppSide.Client)
            {
                if (fuseSound == null)
                {
                    fuseSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams
                    {
                        Location = new AssetLocation("game:sounds/effect/fuse"),
                        ShouldLoop = true,
                        Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = 0.1f,
                        Range = 16f
                    });
                }
            }
        }

        public void OnIgnite(IPlayer byPlayer)
        {
            if (lit) return;

            if (Api.Side == EnumAppSide.Client) fuseSound?.Start();

            lit = true;
            RemainingSeconds = 4f;
            ignitedByPlayerUid = byPlayer?.PlayerUID;
            MarkDirty(false);
        }

        private void OnTick(float dt)
        {
            if (!lit) return;

            RemainingSeconds -= dt;

            if (Api.Side == EnumAppSide.Server && RemainingSeconds <= 0f)
            {
                Combust();
            }

            if (Api.Side == EnumAppSide.Client)
            {
                BlockEntityBomb.smallSparks.MinPos.Set(Pos.X + 0.45, Pos.Y + 0.53, Pos.Z + 0.45);
                Api.World.SpawnParticles(BlockEntityBomb.smallSparks, null);
            }
        }

        private void Combust()
        {
            if (requireClaimPermission && !HasPermissionToUse())
            {
                Api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/extinguish*"), Pos, -0.5);
                lit = false;
                MarkDirty(true);
                return;
            }
            Api.World.BlockAccessor.SetBlock(0, Pos);
            BreachArea();
            Api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/smallexplosion"), Pos, -0.5);
            if (knockbackStrength > 0f)
            {
                var center = Pos.ToVec3d().Add(0.5, 0.5, 0.5);
                var ents = Api.World.GetEntitiesAround(center, (float)scanRadius, (float)scanRadius);

                foreach (var ent in ents)
                {
                    var dir = ent.ServerPos.XYZ - center;
                    double len = dir.Length();
                    if (len < 0.0001) continue;
                    dir.X /= len; dir.Y /= len; dir.Z /= len;

                    ent.ServerPos.Motion.Add(
                        dir.X * knockbackStrength,
                        dir.Y * knockbackStrength,
                        dir.Z * knockbackStrength
                    );
                }
            }
        }

        private bool HasPermissionToUse()
        {
            int rad = (int)Math.Ceiling(scanRadius);
            var exploArea = new Cuboidi(Pos.AddCopy(-rad, -rad, -rad), Pos.AddCopy(rad, rad, rad));
            var claims = (Api as ICoreServerAPI).WorldManager.LandClaims;
            var player = Api.World.PlayerByUid(ignitedByPlayerUid);
            for (int i = 0; i < claims.Count; i++)
            {
                if (claims[i].Intersects(exploArea))
                {
                    return claims[i].TestPlayerAccess(player, EnumBlockAccessFlags.BuildOrBreak) > EnumPlayerAccessResult.Denied;
                }
            }
            return true;
        }

        private void BreachArea()
        {
            var sapi = Api as ICoreServerAPI;
            var world = Api.World;
            var modSys = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>(true);

            int r = (int)Math.Ceiling(scanRadius);
            var center = Pos;
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dz = -r; dz <= r; dz++)
                    {
                        var p = new BlockPos(center.X + dx, center.Y + dy, center.Z + dz);
                        if (center.DistanceTo(p) > scanRadius) continue;

                        var bre = modSys.GetReinforcment(p);
                        if (bre == null) continue;

                        bool allow = ModConfig.Instance.Main.ExplosionsAffectPlayerReinforcement
                                     || bre.PlayerUID == "010100110100111101010011";

                        int damage = 0;
                        if (allow)
                        {
                            damage = RollDamage(Api.World.Rand, reinforcementDamageRoll.min, reinforcementDamageRoll.max);
                        }
                        if (damage > 0)
                        {
                            modSys.ConsumeStrength(p, damage);
                        }

                        if (!allow) continue;
                        var be = world.BlockAccessor.GetBlockEntity(p);
                        if (be != null)
                        {
                            TryExplosionDamageContainerContents(world, p, containerDamageChance);

                            if (unlockLocks)
                            {
                                var lockData = be.GetBehavior<BlockEntityThieveryLockData>();
                                if (lockData != null)
                                {
                                    bool hasLock = !string.IsNullOrEmpty(lockData.LockUID)
                                                   || !string.IsNullOrEmpty(lockData.LockType)
                                                   || bre.Locked;

                                    if (hasLock)
                                    {
                                        lockData.LockedState = false;
                                        lockData.LockUID = null;
                                        lockData.LockType = null;
                                        be.MarkDirty(true);

                                        sapi?.Network?.BroadcastBlockEntityPacket(p,
                                            ThieveryPacketIds.LockStateSync,
                                            new LockData { LockUid = null, LockType = null, IsLocked = false });
                                    }
                                }
                            }

                            world.BlockAccessor.MarkBlockEntityDirty(p);
                        }

                        world.BlockAccessor.MarkBlockDirty(p, (IPlayer)null);
                    }
                }
            }
        }

        private static int RollDamage(System.Random rand, int min, int max)
        {
            if (min <= 0 && max <= 0) return 0;
            if (min > max) (min, max) = (max, min);
            return rand.Next(min, max + 1);
        }

        private static (int, int) ParseRange(string s)
        {
            if (string.IsNullOrEmpty(s)) return (1, 3);
            var parts = s.Split('-');
            if (parts.Length != 2) return (1, 3);
            if (int.TryParse(parts[0], out int a) && int.TryParse(parts[1], out int b)) return (a, b);
            return (1, 3);
        }

        private static void TryExplosionDamageContainerContents(IWorldAccessor world, BlockPos pos, float chance)
        {
            if (chance <= 0f) return;

            var be = world.BlockAccessor.GetBlockEntity(pos);
            if (be == null) return;

            bool changed = false;

            if (be is BlockEntityContainer bec && bec.Inventory != null)
            {
                for (int i = 0; i < bec.Inventory.Count; i++)
                {
                    var slot = bec.Inventory[i];
                    if (slot?.Empty == false && world.Rand.NextDouble() < chance)
                    {
                        slot.TakeOutWhole();
                        slot.MarkDirty();
                        changed = true;
                    }
                }
                if (changed) bec.MarkDirty(true);
                return;
            }

            if (be is BlockEntityGenericTypedContainer beg && beg.Inventory != null)
            {
                for (int i = 0; i < beg.Inventory.Count; i++)
                {
                    var slot = beg.Inventory[i];
                    if (slot?.Empty == false && world.Rand.NextDouble() < chance)
                    {
                        slot.TakeOutWhole();
                        slot.MarkDirty();
                        changed = true;
                    }
                }
                if (changed) beg.MarkDirty(true);
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            RemainingSeconds = tree.GetFloat("remainingSeconds", 0f);
            lit = tree.GetInt("lit", 0) > 0;
            ignitedByPlayerUid = tree.GetString("ignitedByPlayerUid", null);
            if (!lit && Api?.Side == EnumAppSide.Client) fuseSound?.Stop();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("remainingSeconds", RemainingSeconds);
            tree.SetInt("lit", lit ? 1 : 0);
            tree.SetString("ignitedByPlayerUid", ignitedByPlayerUid);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            fuseSound?.Stop();
        }
    }
}
