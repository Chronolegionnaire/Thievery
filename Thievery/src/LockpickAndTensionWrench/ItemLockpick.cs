using System;
using System.Collections.Generic;
using Thievery.Config;
using Thievery.LockAndKey;
using Thievery.src.LockpickAndTensionWrench;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Thievery.LockpickAndTensionWrench
{
    public class ItemLockpick : Item
    {
        private int PadlockDifficulty = 10000;
        private ICoreAPI api;
        private LockManager lockManager;
        
        private int GetLockDifficulty(string lockType)
        {
            var diff = ModConfig.Instance?.Difficulty;

            var padlockDifficulties = new Dictionary<string, int>
            {
                { "padlock-blackbronze",     diff?.BlackBronzePadlockDifficulty ?? 25 },
                { "padlock-bismuthbronze",   diff?.BismuthBronzePadlockDifficulty ?? 30 },
                { "padlock-tinbronze",       diff?.TinBronzePadlockDifficulty ?? 35 },
                { "padlock-iron",            diff?.IronPadlockDifficulty ?? 60 },
                { "padlock-meteoriciron",    diff?.MeteoricIronPadlockDifficulty ?? 70 },
                { "padlock-steel",           diff?.SteelPadlockDifficulty ?? 80 },
                { "padlock-copper",          diff?.CopperPadlockDifficulty ?? 10 },
                { "padlock-nickel",          diff?.NickelPadlockDifficulty ?? 15 },
                { "padlock-silver",          diff?.SilverPadlockDifficulty ?? 35 },
                { "padlock-gold",            diff?.GoldPadlockDifficulty ?? 20 },
                { "padlock-titanium",        diff?.TitaniumPadlockDifficulty ?? 90 },
                { "padlock-lead",            diff?.LeadPadlockDifficulty ?? 20 },
                { "padlock-zinc",            diff?.ZincPadlockDifficulty ?? 25 },
                { "padlock-tin",             diff?.TinPadlockDifficulty ?? 20 },
                { "padlock-chromium",        diff?.ChromiumPadlockDifficulty ?? 60 },
                { "padlock-cupronickel",     diff?.CupronickelPadlockDifficulty ?? 40 },
                { "padlock-electrum",        diff?.ElectrumPadlockDifficulty ?? 20 },
                { "padlock-platinum",        diff?.PlatinumPadlockDifficulty ?? 50 },
            };

            if (padlockDifficulties.TryGetValue(lockType, out int difficulty))
            {
                return Math.Clamp(difficulty, 1, 100);
            }
            return 50;
        }

        private bool ShouldUseBindingOrder(int lockDifficulty)
            => lockDifficulty >= (ModConfig.Instance?.MiniGame?.BindingOrderThreshold ?? 75);
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
        private static readonly HashSet<EnumPlayerAccessResult> AccessGranted =
            new HashSet<EnumPlayerAccessResult>
            {
                EnumPlayerAccessResult.OkOwner,
                EnumPlayerAccessResult.OkGroup,
                EnumPlayerAccessResult.OkPrivilege,
                EnumPlayerAccessResult.OkGrantedPlayer,
                EnumPlayerAccessResult.OkGrantedGroup
            };

        private bool HasUseAccessInClaims(IPlayer player, BlockPos pos)
        {
            var claimsApi = api?.World?.Claims;
            if (claimsApi == null) return true; // no claim system → allow

            // Client: TestAccess/TryAccess always returns true, so we must inspect claims directly
            var claims = claimsApi.Get(pos);
            if (claims == null || claims.Length == 0) return true; // not in a claim

            foreach (var claim in claims)
            {
                var res = claim.TestPlayerAccess(player, EnumBlockAccessFlags.Use);
                if (AccessGranted.Contains(res)) return true;
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
            if (ModConfig.Instance?.Main?.BlockLockpickOnLandClaims == true)
            {
                if (api.Side == EnumAppSide.Server)
                {
                    if (!api.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use))
                    {
                        handling = EnumHandHandling.PreventDefault;
                        return;
                    }
                }
                else
                {
                    if (!HasUseAccessInClaims(player, blockSel.Position))
                    {
                        (api as ICoreClientAPI)?.TriggerIngameError(
                            "thieverymod-landclaim",
                            "landclaimlockpickblocked",
                            Lang.Get("thievery:lockpick-blocked-on-claim")
                        );
                        handling = EnumHandHandling.PreventDefault;
                        return;
                    }
                }
            }
            if (api.Side == EnumAppSide.Client)
            {
                var capi = api as ICoreClientAPI;
                if (capi != null)
                {
                    bool lockpickingDialogOpen = false;
                    foreach (var gui in capi.Gui.OpenedGuis)
                    {
                        if (gui is GuiLockpickingMiniGame)
                        {
                            lockpickingDialogOpen = true;
                            break;
                        }
                    }

                    if (lockpickingDialogOpen)
                    {
                        capi.TriggerIngameError("thieverymod-dialog", "dialogalreadyopen",
                            Lang.Get("thievery:dialog-close-current"));
                        handling = EnumHandHandling.PreventDefault;
                        return;
                    }
                }
            }
            
            CharacterSystem characterSystem = api.ModLoader.GetModSystem<CharacterSystem>();
            var requiredTraits = ModConfig.Instance?.Main?.RequiredTraits;

            if (requiredTraits != null && requiredTraits.Count > 0)
            {
                bool hasAnyTrait = false;

                foreach (var trait in requiredTraits)
                {
                    if (characterSystem.HasTrait(player, trait))
                    {
                        hasAnyTrait = true;
                        break;
                    }
                }

                if (!hasAnyTrait)
                {
                    (api as ICoreClientAPI)?.TriggerIngameError("thieverymod-traitcheck", "missingtrait",
                        Lang.Get("thievery:trait-missing-lockpick"));
                    handling = EnumHandHandling.PreventDefault;
                    return;
                }
            }

            if (!IsTensionWrenchInOffHand(byEntity))
            {
                (api as ICoreClientAPI)?.TriggerIngameError("thieverymod-tensionwrench", "notensionwrench",
                    Lang.Get("thievery:need-tension-wrench"));
                handling = EnumHandHandling.PreventDefault;
                return;
            }

            var lockData = lockManager.GetLockData(blockSel.Position);
            if (lockData == null)
            {
                (api as ICoreClientAPI)?.TriggerIngameError("thieverymod-lockable", "nolock",
                    Lang.Get("thievery:not-lockable"));
                handling = EnumHandHandling.PreventDefault;
                return;
            }
            bool hasActualLock =
                lockData != null &&
                !string.IsNullOrEmpty(lockData.LockUid) &&
                !string.IsNullOrEmpty(lockData.LockType);

            if (!hasActualLock)
            {
                (api as ICoreClientAPI)?.TriggerIngameError("thieverymod-nopadlock", "nopadlock",
                    Lang.Get("thievery:no-padlock"));
                handling = EnumHandHandling.PreventDefault;
                return;
            }
            if (ModConfig.Instance.MiniGame.LockpickingMinigame)
            {
                PadlockDifficulty = GetLockDifficulty(lockData.LockType);
                if (api.Side == EnumAppSide.Client)
                {
                    ICoreClientAPI capi = api as ICoreClientAPI;
                    var minigameDialog = new GuiLockpickingMiniGame(
                        Lang.Get("thievery:ui-lockpicking-title"),
                        blockSel.Position,
                        capi,
                        PadlockDifficulty,
                        ShouldUseBindingOrder(PadlockDifficulty),
                        lockData.LockType
                    );
                    byEntity.Controls.RightMouseDown = false;
                    capi.Input.MouseWorldInteractAnyway = true;
                    minigameDialog.OnComplete += () =>
                    {
                        capi.Network.GetChannel("thievery").SendPacket(new LockPickCompletePacket
                        {
                            BlockPos = blockSel.Position,
                            LockUid = lockData.LockUid,
                            Action = LockAction.Toggle
                        });
                    };
                    minigameDialog.OnClosed += () =>
                    {
                        capi.Input.MouseWorldInteractAnyway = false;
                    };

                    minigameDialog.TryOpen();
                }

                handling = EnumHandHandling.PreventDefault;
                return;
            }

            PadlockDifficulty = GetLockDifficulty(lockData.LockType);
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
                ICoreClientAPI capiClient = api as ICoreClientAPI;
                if (pickData.LockpickingSound != null)
                {
                    pickData.LockpickingSound.Stop();
                    pickData.LockpickingSound.Dispose();
                    pickData.LockpickingSound = null;
                }

                pickData.LockpickingSound = capiClient.World.LoadSound(new SoundParams()
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
            float progress = Math.Min(1f, (float)elapsedTime / PadlockDifficulty * 2000);

            if (elapsedTime % 2000 < 100)
            {
                Random random = new Random();
                bool lockpickBroke = false;

                if (random.NextDouble() < ModConfig.Instance.Main.LockPickDamageChance)
                {
                    lockpickBroke = DamageItem(slot, (int)ModConfig.Instance.Main.LockPickDamage, byEntity);
                }

                var offhandSlot = (byEntity as EntityAgent)?.LeftHandItemSlot;
                if (!lockpickBroke && offhandSlot?.Itemstack?.Collectible != null &&
                    random.NextDouble() < ModConfig.Instance.Main.LockPickDamageChance)
                {
                    DamageItem(offhandSlot, (int)ModConfig.Instance.Main.LockPickDamage, byEntity);
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

            if (elapsedTime >= PadlockDifficulty * 2000)
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
            if (lockData != null)
            {
                if (api.Side == EnumAppSide.Client)
                {
                    var clientApi = api as ICoreClientAPI;
                    StopPicking(player, pickData);
                    clientApi.Network.GetChannel("thievery").SendPacket(new LockPickCompletePacket
                    {
                        BlockPos = blockSel.Position,
                        LockUid = lockData.LockUid,
                        Action = LockAction.Toggle
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