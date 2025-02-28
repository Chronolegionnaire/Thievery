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
                { "padlock-steel", (int)(Config.SteelPadlockPickDurationSeconds * 1000) },
                { "padlock-copper", (int)(Config.CopperPadlockPickDurationSeconds * 1000) },
                { "padlock-nickel", (int)(Config.NickelPadlockPickDurationSeconds * 1000) },
                { "padlock-silver", (int)(Config.SilverPadlockPickDurationSeconds * 1000) },
                { "padlock-gold", (int)(Config.GoldPadlockPickDurationSeconds * 1000) },
                { "padlock-titanium", (int)(Config.TitaniumPadlockPickDurationSeconds * 1000) },
                { "padlock-lead", (int)(Config.LeadPadlockPickDurationSeconds * 1000) },
                { "padlock-zinc", (int)(Config.ZincPadlockPickDurationSeconds * 1000) },
                { "padlock-tin", (int)(Config.TinPadlockPickDurationSeconds * 1000) },
                { "padlock-chromium", (int)(Config.ChromiumPadlockPickDurationSeconds * 1000) },
                { "padlock-cupronickel", (int)(Config.CupronickelPadlockPickDurationSeconds * 1000) },
                { "padlock-electrum", (int)(Config.ElectrumPadlockPickDurationSeconds * 1000) },
                { "padlock-platinum", (int)(Config.PlatinumPadlockPickDurationSeconds * 1000) }
            };

            if (lockPickDurations.TryGetValue(lockType, out int duration))
            {
                return duration;
            }
            return (int)(Config.BlackBronzePadlockPickDurationSeconds * 1000);
        }
        public class PlayerPickData
        {
            public bool IsPicking = false;
            public long PickStartTime = 0;
            public ILoadedSound LockpickingSound;
        }
        
        public void StopPickingForPlayer(IPlayer player)
        {
            if (player == null) return;
        
            string playerUid = player.PlayerUID;
            if (pickDataByPlayerUid.TryGetValue(playerUid, out PlayerPickData pickData) && pickData.IsPicking)
            {
                StopPicking(player, pickData);
            }
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
            if (api.Side == EnumAppSide.Server)
            {
                api.World.PlaySoundAt(
                    new AssetLocation("thievery", "sounds/lockpicking"),
                    blockSel.Position,
                    0.5,
                    player,
                    true,
                    16f, 
                    1f 
                );
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

            string playerUid = player.PlayerUID;
            if (!pickDataByPlayerUid.TryGetValue(playerUid, out PlayerPickData pickData) || !pickData.IsPicking)
            {
                return false;
            }

            if (!IsTensionWrenchInOffHand(byEntity))
            {
                StopPicking(player, pickDataByPlayerUid[player.PlayerUID]);
                return false;
            }

            long elapsedTime = api.World.ElapsedMilliseconds - pickData.PickStartTime;
            float progress = Math.Min(1f, (float)elapsedTime / FlatPickDurationMs);

            if (elapsedTime % 2000 < 100)
            {
                Random random = new Random();
                bool lockpickBroke = false;

                if (random.NextDouble() < Config.LockPickDamageChance)
                {
                    lockpickBroke = DamageItem(slot, (int)Config.LockPickDamage, byEntity);
                }

                var offhandSlot = (byEntity as EntityAgent)?.LeftHandItemSlot;
                if (!lockpickBroke && offhandSlot?.Itemstack?.Collectible != null &&
                    random.NextDouble() < Config.LockPickDamageChance)
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

                if (itemSlot == null || itemSlot.Itemstack == null || itemSlot.Itemstack.Collectible == null)
                {
                    return false;
                }
                int durability = itemSlot.Itemstack.Attributes.GetInt("durability", 0);
                bool willBreak = durability <= damage;
                var clientApi = api as ICoreClientAPI;
                clientApi.Network.GetChannel("thievery").SendPacket(new ItemDamagePacket
                {
                    InventoryId = itemSlot.Inventory.InventoryID,
                    SlotId = itemSlot.Inventory.GetSlotId(itemSlot),
                    Damage = damage
                });
                return willBreak;
            }
            catch (Exception ex)
            {
                api.World.Logger.Error("DamageItem: Exception during DamageItem. {0}", ex);
                return false;
            }
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
                if (api.Side == EnumAppSide.Client)
                {
                    var clientApi = api as ICoreClientAPI;
                    StopPicking(player, pickData);
                    clientApi.Network.GetChannel("thievery").SendPacket(new LockPickCompletePacket
                    {
                        BlockPos = blockSel.Position,
                        LockUid = lockData.LockUid
                    });
                }
                StopPicking(player, pickData);
            }
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
                var modSystem = clientApi?.ModLoader?.GetModSystem<ThieveryModSystem>();
                if (modSystem != null && modSystem.LockpickHudElement != null)
                {
                    modSystem.LockpickHudElement.CircleVisible = false;
                    modSystem.LockpickHudElement.CircleProgress = 0f;
                }
            }
        }
    }
}