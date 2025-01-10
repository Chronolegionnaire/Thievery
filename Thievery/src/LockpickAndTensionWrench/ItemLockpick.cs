using System;
using System.Collections.Generic;
using Thievery.LockAndKey;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Thievery.LockpickAndTensionWrench
{
    public class ItemLockpick : Item
    {
        private int FlatPickDurationMs = 10000;
        private ICoreAPI api;
        private LockManager lockManager;

        private Config.Config Config => ThieveryModSystem.LoadedConfig;

        private int GetPickDuration(string lockType)
        {
            var lockPickDurations = new Dictionary<string, int>
            {
                { "padlock-blackbronze", (int)(Config.BlackBronzePadlockPickDurationSeconds * 1000) },
                { "padlock-bismuthbronze", (int)(Config.BismuthBronzePadlockPickDurationSeconds * 1000) },
                { "padlock-tinbronze", (int)(Config.TinBronzePadlockPickDurationSeconds * 1000) },
                { "padlock-iron", (int)(Config.IronPadlockPickDurationSeconds * 1000) },
                { "padlock-meteoriciron", (int)(Config.MeteoricIronPadlockPickDurationSeconds * 1000) },
                { "padlock-steel", (int)(Config.SteelPadlockPickDurationSeconds * 1000) }
            };

            if (lockPickDurations.TryGetValue(lockType, out int duration))
            {
                return duration;
            }
            return (int)(Config.BlackBronzePadlockPickDurationSeconds * 1000);
        }


        private class PlayerPickData
        {
            public bool IsPicking = false;
            public long PickStartTime = 0;
            public ILoadedSound LockpickingSound;
        }

        private Dictionary<string, PlayerPickData> pickDataByPlayerUid = new Dictionary<string, PlayerPickData>();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.api = api;
            this.lockManager = new LockManager(api);
        }

        private bool IsTensionWrenchInOffHand(Entity entity)
        {
            if (entity is EntityAgent entityAgent)
            {
                ItemSlot offhandSlot = entityAgent.LeftHandItemSlot;
                if (offhandSlot != null && offhandSlot.Itemstack?.Collectible?.Code != null)
                {
                    string code = offhandSlot.Itemstack.Collectible.Code.Path;
                    if (code.StartsWith("tensionwrench-"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override void OnHeldInteractStart(
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel,
            bool firstEvent,
            ref EnumHandHandling handling)
        {
            if (blockSel == null) return;

            var player = (byEntity as EntityPlayer)?.Player;
            if (player == null) return;

            if (!byEntity.Controls.Sneak) return;
            CharacterSystem characterSystem = api.ModLoader.GetModSystem<CharacterSystem>();
            if (Config.RequiresPilferer && !characterSystem.HasTrait(player, "pilferer") &&
                Config.RequiresTinkerer && !characterSystem.HasTrait(player, "tinkerer"))
            {
                var capi = api as ICoreClientAPI;
                capi?.TriggerIngameError("thieverymod-traitcheck", "missingtrait",
                    "You do not know how to use a lockpick!");
                return;
            }

            if (!IsTensionWrenchInOffHand(byEntity))
            {
                var capi = api as ICoreClientAPI;
                capi?.TriggerIngameError("thieverymod-tensionwrench", "notensionwrench",
                    "You need a tension wrench in your off-hand to use the lockpick!");
                return;
            }

            var lockData = lockManager.GetLockData(blockSel.Position);
            if (lockData == null || !lockData.IsLocked)
            {
                var capi = api as ICoreClientAPI;
                capi?.TriggerIngameError("thieverymod-lockable", "nolock",
                    "This block is not lockable or is already unlocked!");
                return;
            }

            FlatPickDurationMs = GetPickDuration(lockData.LockType);

            string playerUid = player.PlayerUID;
            if (!pickDataByPlayerUid.TryGetValue(playerUid, out PlayerPickData pickData))
            {
                pickData = new PlayerPickData();
                pickDataByPlayerUid[playerUid] = pickData;
            }

            pickData.IsPicking = true;
            pickData.PickStartTime = api.World.ElapsedMilliseconds;

            if (api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = api as ICoreClientAPI;
                if (pickData.LockpickingSound != null)
                {
                    pickData.LockpickingSound.Stop();
                    pickData.LockpickingSound.Dispose();
                    pickData.LockpickingSound = null;
                }

                pickData.LockpickingSound = capi.World.LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("thievery", "sounds/lockpicking"),
                    Position = new Vec3f(
                        (float)(blockSel.Position.X + 0.5),
                        (float)(blockSel.Position.Y + 0.5),
                        (float)(blockSel.Position.Z + 0.5)
                    ),
                    DisposeOnFinish = false,
                    Pitch = 1.0f,
                    Volume = 1.0f,
                    Range = 16f,
                    ShouldLoop = true
                });

                pickData.LockpickingSound?.Start();
            }

            handling = EnumHandHandling.PreventDefault;
        }

        public override bool OnHeldInteractStep(
            float secondsUsed,
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel)
        {
            if (api.Side != EnumAppSide.Client)
            {
                return false;
            }
            if (blockSel == null) return false;
            var player = (byEntity as EntityPlayer)?.Player;
            if (player == null || !byEntity.Controls.Sneak) return false;
            if (!IsTensionWrenchInOffHand(byEntity))
            {
                StopPicking(player, pickDataByPlayerUid[player.PlayerUID]);
                return false;
            }

            string playerUid = player.PlayerUID;
            if (!pickDataByPlayerUid.TryGetValue(playerUid, out PlayerPickData pickData) || !pickData.IsPicking)
            {
                return false;
            }
            long elapsedTime = api.World.ElapsedMilliseconds - pickData.PickStartTime;
            float progress = Math.Min(1f, (float)elapsedTime / FlatPickDurationMs);
            if (elapsedTime % 2000 < 100)
            {
                Random random = new Random();
                if (random.NextDouble() < Config.LockPickDamageChance)
                {
                    bool lockpickDestroyed = DamageItem(slot, (int)Config.LockPickDamage, byEntity);
                    if (lockpickDestroyed)
                    {
                        StopPicking(player, pickData);
                        return false;
                    }
                }
                var offhandSlot = (byEntity as EntityAgent)?.LeftHandItemSlot;
                if (offhandSlot?.Itemstack?.Collectible != null && random.NextDouble() < Config.LockPickDamageChance)
                {
                    DamageItem(offhandSlot, (int)Config.LockPickDamage, byEntity);
                }
            }

            if (api.Side == EnumAppSide.Client)
            {
                var clientApi = api as ICoreClientAPI;
                var hudElement = clientApi?.ModLoader?.GetModSystem<ThieveryModSystem>()?.LockpickHudElement;
                if (hudElement != null)
                {
                    hudElement.CircleProgress = progress;
                    hudElement.CircleVisible = true;
                }
            }

            if (elapsedTime >= FlatPickDurationMs)
            {
                CompletePicking(player, blockSel, pickData);
            }

            return true;
        }

        private bool DamageItem(ItemSlot itemSlot, int damage, EntityAgent byEntity)
        {
            try
            {
                if (api.Side != EnumAppSide.Client)
                {
                    return false;
                }
                if (itemSlot == null)
                {
                    return false;
                }

                if (itemSlot.Itemstack == null)
                {
                    return false;
                }

                if (itemSlot.Itemstack.Collectible == null)
                {
                    return false;
                }
                var originalItemstack = itemSlot.Itemstack;
                itemSlot.Itemstack.Collectible.DamageItem(api.World, byEntity, itemSlot, damage);
                if (itemSlot.Itemstack == null || itemSlot.Itemstack != originalItemstack)
                {
                    itemSlot.MarkDirty();
                    return true;
                }
                itemSlot.MarkDirty();
            }
            catch (Exception ex)
            {
                api.World.Logger.Error("DamageItem: Exception during DamageItem. {0}", ex);
            }

            return false;
        }
        public override void OnHeldInteractStop(
            float secondsUsed,
            ItemSlot slot,
            EntityAgent byEntity,
            BlockSelection blockSel,
            EntitySelection entitySel)
        {
            var player = (byEntity as EntityPlayer)?.Player;
            if (player == null) return;

            string playerUid = player.PlayerUID;
            if (pickDataByPlayerUid.TryGetValue(playerUid, out PlayerPickData pickData) && pickData.IsPicking)
            {
                StopPicking(player, pickData);
            }
        }

        private void CompletePicking(IPlayer player, BlockSelection blockSel, PlayerPickData pickData)
        {
            var lockData = lockManager.GetLockData(blockSel.Position);
            if (lockData != null && lockData.IsLocked)
            {
                lockManager.SetLock(blockSel.Position, lockData.LockUid, false);

                api.World.PlaySoundAt(
                    new AssetLocation("thievery:sounds/lock"),
                    blockSel.Position.X + 0.5,
                    blockSel.Position.Y + 0.5,
                    blockSel.Position.Z + 0.5,
                    player,
                    true,
                    32f,
                    1f
                );
            }

            StopPicking(player, pickData);
        }

        private void StopPicking(IPlayer player, PlayerPickData pickData)
        {
            if (pickData == null) return;

            pickData.IsPicking = false;
            pickData.PickStartTime = 0;

            if (api.Side == EnumAppSide.Client)
            {
                if (pickData.LockpickingSound != null)
                {
                    pickData.LockpickingSound.Stop();
                    pickData.LockpickingSound.Dispose();
                    pickData.LockpickingSound = null;
                }

                var clientApi = api as ICoreClientAPI;
                var hudElement = clientApi?.ModLoader?.GetModSystem<ThieveryModSystem>()?.LockpickHudElement;
                if (hudElement != null)
                {
                    hudElement.CircleVisible = false;
                }
            }
        }
    }
}