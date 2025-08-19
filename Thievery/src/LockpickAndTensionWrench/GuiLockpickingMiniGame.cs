using System;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using System.Collections.Generic;
using System.IO;
using Thievery.LockpickAndTensionWrench;
using HarmonyLib;
using Thievery.Config;
using Thievery.LockAndKey;
using Vintagestory.API.Config;

namespace Thievery.src.LockpickAndTensionWrench
{
    public class GuiLockpickingMiniGame : GuiDialogBlockEntity
    {
        public event Action OnComplete;
        private SingleMeshGuiRenderer lockRenderer;
        private SingleMeshGuiRenderer wrenchRenderer;
        private SingleMeshGuiRenderer pickRenderer;
        private const int MaxPinCount = 7;
        private const int MaxPinCode = 6;
        const string DefaultMetal = "steel";
        private int difficultyLevel;
        private bool bindingOrder;
        private string lockType;
        private long guiOpenedTime;
        private const long rightClickDelay = 200;
        private bool[] pinInitiallyDisabled;
        private float pickXBase = -78f;
        private float pickXDelta = 25f;
        private float pickZBase  = -1036f;
        private float pickZDelta = -25f;
        private float pickDepthLerpPerSec = 12f;
        private float pickTargetX, pickTargetZ;
        private float pickPitchBaseDeg   = 360f;
        private float pickPitchMaxDelta  = -20f;
        private float pickTiltLerpPerSec = 12f;
        private float pickYawBaseDeg     = -229f;
        private float pickYawMaxDelta    = -0f;
        private float pickYawLerpPerSec  = 12f;
        private float pickRollBaseDeg    = 2f;
        private float pickRollMaxDelta   = -20f;
        private float pickRollLerpPerSec = 12f;

        private ILoadedSound sLoopInteract, sHotspot, sFalse, sSet, sSelect;
        
        private HashSet<int>[] pinFalseSetCodes;
        private int[] lastClickPos;
        private BlockPos bePos;
        private string sessionTokenActive;
        private string sessionTokenOffhand;
        private const string SessionAttrKey = "thievery.lockpickSessionToken";
        private ItemSlot initialActiveSlot;
        private ItemSlot initialOffhandSlot;
        private AssetLocation initialActiveCode;
        private AssetLocation initialOffhandCode;
        private bool tokenEnforcementActive;
        private long tokenEnforceStartMs;
        private const int tokenEnforceDelayMs = 400;
        private double missUnsetChance;
        private double falseUnsetChance;
        private double bindingChance;  
        private double D01 => GameMath.Clamp((difficultyLevel - 10) / 85.0, 0.0, 1.0);
        private bool[] pinIsBinding;

        private Random Rng => capi?.World?.Rand ?? Random.Shared;
        public override bool ShouldReceiveKeyboardEvents() => true;
        enum PinStates
        {
            Still,
            Down,
            Up,
            Locked
        }

        private const double dialogWidth = 640, dialogHeight = 520;
        private const double drawingX = 320, drawingY = 0, lockWidth = 320, lockHeight = 520;

        private Dictionary<string, Color> metalColorMapping = new Dictionary<string, Color>
    {
        { "blackbronze", new Color(0.18, 0.12, 0.13) },
        { "bismuthbronze", new Color(0.57, 0.29, 0.14) },
        { "tinbronze", new Color(0.5, 0.4, 0.3) },
        { "iron", new Color(0.7, 0.7, 0.7) },
        { "meteoriceron", new Color(0.85, 0.82, 0.73) },
        { "steel", new Color(0.6, 0.6, 0.6) },
        { "copper", new Color(0.9, 0.5, 0.3) },
        { "nickel", new Color(0.75, 0.75, 0.75) },
        { "silver", new Color(0.9, 0.9, 0.9) },
        { "gold", new Color(1.0, 0.84, 0.0) },
        { "titanium", new Color(0.8, 0.8, 0.9) },
        { "lead", new Color(0.4, 0.4, 0.4) },
        { "zinc", new Color(0.5, 0.5, 0.5) },
        { "tin", new Color(0.6, 0.6, 0.6) },
        { "chromium", new Color(0.7, 0.7, 0.8) },
        { "cupronickel", new Color(0.68, 0.49, 0.36) },
        { "electrum", new Color(0.9, 0.8, 0.6) },
        { "platinum", new Color(0.9, 0.9, 1.0) }
    };


        private ImageSurface lockTextureSurface;
        private Color GetLockColor()
        {
            string metal = lockType.Replace("padlock-", "");
            if (metalColorMapping.TryGetValue(metal, out Color color))
            {
                return color;
            }
            return new Color(0.671, 0.514, 0.039);
        }

        public GuiLockpickingMiniGame(
            string dialogTitle,
            BlockPos blockEntityPos,
            ICoreClientAPI capi,
            int padlockDifficulty,
            bool bindingOrder,
            string lockType)
            : base(dialogTitle, blockEntityPos, capi)
        {
            this.capi = capi;
            this.bindingOrder = bindingOrder;
            this.difficultyLevel = padlockDifficulty;
            this.lockType = lockType;
            this.bePos = blockEntityPos;

            SetupDifficultyScaling();
            SetupDialog();
            SetupGame();
        }

        private PinStates[] pinStates;
        private int[] pinCodes;
        private int[]? pinBindingOrder = null;

        private void SetupDifficultyScaling()
        {
            var pins = CalculatePinCount();

            pinStates = new PinStates[MaxPinCount];
            pinCodes = new int[MaxPinCount];
            pinInitiallyDisabled = new bool[MaxPinCount];

            for (int i = 0; i < MaxPinCount; ++i)
            {
                if (i < pins)
                {
                    pinStates[i] = PinStates.Still;
                    pinCodes[i] = capi.World.Rand.Next(1, MaxPinCode + 1);
                    pinInitiallyDisabled[i] = false;
                }
                else
                {
                    pinStates[i] = PinStates.Locked;
                    pinCodes[i] = MaxPinCode;
                    pinInitiallyDisabled[i] = true;
                }
            }

            if (bindingOrder)
            {
                pinBindingOrder = Enumerable.Range(0, MaxPinCount).ToArray();
                GameMath.Shuffle(Random.Shared, pinBindingOrder);
            }
            int baseFalseSetsPerPin = (int)GameMath.Clamp(Math.Floor(difficultyLevel / 30.0), 0, MaxPinCode - 1);

            pinFalseSetCodes = new HashSet<int>[MaxPinCount];
            lastClickPos = new int[MaxPinCount];

            for (int i = 0; i < MaxPinCount; i++)
            {
                pinFalseSetCodes[i] = new HashSet<int>();
                lastClickPos[i] = -1;

                if (pinInitiallyDisabled[i]) continue;
                int desiredFalseSets = baseFalseSetsPerPin + (capi.World.Rand.NextDouble() < 0.3 ? 1 : 0);
                desiredFalseSets = GameMath.Clamp(desiredFalseSets, 0, MaxPinCode - 1);

                int tries = 0;
                while (pinFalseSetCodes[i].Count < desiredFalseSets && tries++ < 30)
                {
                    int code = capi.World.Rand.Next(1, MaxPinCode + 1);
                    if (code != pinCodes[i]) pinFalseSetCodes[i].Add(code);
                }
            }
            bindingChance = GameMath.Clamp(0.30 + D01 * 0.45, 0.30, 0.75);

            pinIsBinding = new bool[MaxPinCount];
            RebindPins();
            missUnsetChance = GameMath.Clamp(0.15 + D01 * 0.30, 0.05, 0.60);
            falseUnsetChance = GameMath.Clamp(0.35 + D01 * 0.40, 0.20, 0.85);
        }

        private int CalculatePinCount()
        {
            if (difficultyLevel < 20) return 3;
            if (difficultyLevel < 40) return 4;
            if (difficultyLevel < 60) return 5;
            if (difficultyLevel < 80) return 6;
            return 7;
        }


        private int pinIndex = 0;
        private bool ignoreLeftUntilUp, ignoreRightUntilUp;
        private bool leftHeld, rightHeld;
        private void StepSelection(int dir)
        {
            if (dir == 0) return;
            int count = pinStates.Length;
            for (int i = 0; i < count; i++)
            {
                pinIndex = (pinIndex + (dir > 0 ? 1 : -1) + count) % count;
                if (!pinInitiallyDisabled[pinIndex]) break;
            }
            PlayOneShot(sSelect);
            RecomputePickDepthTargets();
            SnapPickTiltToCurrent();
            SingleComposer.GetCustomDraw("minigameDraw")?.Redraw();
        }

        private void SetupDialog()
        {
            ElementBounds dialogBounds = ElementBounds.Fixed(0, 0, dialogWidth, dialogHeight)
                .WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds elementBounds = ElementBounds.Fixed(0, 0, dialogWidth, dialogHeight)
                .WithAlignment(EnumDialogArea.CenterMiddle);

            ClearComposers();


            SingleComposer = capi.Gui
                .CreateCompo("lockpickingmingame", dialogBounds)
                .AddDynamicCustomDraw(elementBounds, new DrawDelegateWithBounds(OnMinigameDraw), "minigameDraw")
                .Compose(true);
        }

        private void SetupGame()
        {
            pinPositions = new float[MaxPinCount];
            pinRotations = new float[MaxPinCount];
            lastClickPos = new int[MaxPinCount];
            for (int i = 0; i < MaxPinCount; i++) lastClickPos[i] = -1;
        }

        private float lastUpdateMs;
        private void OnMinigameDraw(Context ctx, ImageSurface surface, ElementBounds elementBounds)
        {
            ctx.Save();
            long currentMs = capi.ElapsedMilliseconds;
            float deltaTime = (currentMs - lastUpdateMs) / 1000f;
            lastUpdateMs = currentMs;
            float timeSec = currentMs / 1000f;
            ctx.Restore();
        }

        private const int pinPadding = 32;

        private float[] pinPositions = new float[MaxPinCount];
        private float[] pinRotations = new float[MaxPinCount];
        private float pinWidth = 32;
        private float pinHeight = 128;

        private int CurrentPinCodePos(int index) => (int)Math.Round(pinPositions[index] * (MaxPinCode + 1), 0);

        private bool PinIsInPosition(int index)
        {
            float bins   = MaxPinCode + 1;
            float pos    = pinPositions[index] * bins;
            float target = pinCodes[index];
            return pinPositions[index] > 0f
                   && Math.Abs(pos - target) <= (0.5f + ModConfig.Instance.MiniGame.HotspotForgivenessBins);
        }

        private long updateCallbackId;
        private float gameTime = 0f;
        private float tensionWrenchDamageTimer = 0f;
        private void Update(float dt)
        {
            if(!IsOpened())
            {
                return;
            }

            gameTime += dt;

            if (capi.World.Player is not null)
            {
                capi.World.Player.Entity.Controls.Sneak = true;
                CheckForRequiredItems();
            }

            tensionWrenchDamageTimer += dt;
            if(tensionWrenchDamageTimer >= 1.0f)
            {
                var damage = (float)Math.Floor(tensionWrenchDamageTimer);
                DamageTensionWrench(difficultyLevel);
                tensionWrenchDamageTimer-=damage;
            }

            for (int i = 0; i < pinPositions.Length; ++i)
            {
                if (pinStates[i] == PinStates.Locked)
                {
                    pinPositions[i] = (float)pinCodes[i] / (float)MaxPinCode;
                }

                if (pinStates[i] == PinStates.Down)
                {
                    pinPositions[i] = (float)Math.Clamp(pinPositions[i] + pinPushSpeed, 0, 1.0);
                    int codePos = CurrentPinCodePos(i);
                    bool atHotspot = (codePos == pinCodes[i]);
                    bool atFalse   = pinFalseSetCodes[i].Contains(codePos);
                    bool nowAtClickZone = atHotspot || atFalse;

                    if (nowAtClickZone && lastClickPos[i] != codePos)
                    {
                        PlayOneShot(atFalse ? sFalse : sHotspot);
                        lastClickPos[i] = codePos;
                    }
                    UpdatePickDepthLerped(dt);
                    UpdatePickTilt(dt);

                }
                else if (pinStates[i] == PinStates.Up)
                {
                    lastClickPos[i] = -1;
                    UpdatePickDepthLerped(dt);
                    UpdatePickTilt(dt);
                }

                else
                {
                    lastClickPos[i] = -1;
                    UpdatePickDepthLerped(dt);
                    UpdatePickTilt(dt);
                }
            }

            var customDraw = SingleComposer.GetCustomDraw("minigameDraw");
            customDraw?.Redraw();

            if (pinStates.All(x => x == PinStates.Locked))
            {
                OnComplete?.Invoke();
                capi.Event.EnqueueMainThreadTask(() => TryClose(), "lockpickingclose");
                return;
            }

            updateCallbackId = capi.Event.RegisterCallback(Update, 16);
        }
        private List<int> CurrentPlayablePins()
        {
            var list = new List<int>();
            for (int i = 0; i < MaxPinCount; i++)
            {
                if (!pinInitiallyDisabled[i] && pinStates[i] != PinStates.Locked)
                    list.Add(i);
            }
            return list;
        }
        private void RebindPins()
        {
            if (pinIsBinding == null) pinIsBinding = new bool[MaxPinCount];

            var playable = CurrentPlayablePins();
            if (playable.Count <= 1)
            {
                foreach (int i in playable) pinIsBinding[i] = false;
                return;
            }
            int nonBindingCount = 0;
            foreach (int i in playable)
            {
                bool bind = Rng.NextDouble() < bindingChance;
                pinIsBinding[i] = bind;
                if (!bind) nonBindingCount++;
            }
            if (nonBindingCount == 0)
            {
                int idx = playable[Rng.Next(playable.Count)];
                pinIsBinding[idx] = false;
                nonBindingCount = 1;
            }
        }
        private void UnbindAll()
        {
            if (pinIsBinding == null) return;
            for (int i = 0; i < MaxPinCount; i++) pinIsBinding[i] = false;
        }
        private bool IsLastPlayablePin(int pin)
        {
            var playable = CurrentPlayablePins();
            return playable.Count == 1 && playable[0] == pin;
        }

        private bool ExistsAnyNonBindingOtherThan(int pin)
        {
            for (int i = 0; i < MaxPinCount; i++)
            {
                if (i == pin) continue;
                if (!pinInitiallyDisabled[i] && pinStates[i] != PinStates.Locked && pinIsBinding[i] == false)
                    return true;
            }
            return false;
        }
        public override bool CaptureRawMouse() => false;
        public override bool ShouldReceiveMouseEvents() => true;

        private bool pushingPin = false;
        private const float pinPushSpeed = 0.01f;
        private float pickPosition = 0.0f;

        public override void OnMouseDown(MouseEvent args)
        {
            if (args.Button != EnumMouseButton.Left) return;

            if (pinInitiallyDisabled[pinIndex])
            {
                args.Handled = true; base.OnMouseDown(args); return;
            }

            if (pinStates[pinIndex] == PinStates.Locked)
            {
                pinStates[pinIndex] = PinStates.Up;
                PlayOneShot(sSet);
                pushingPin = false;
                args.Handled = true; base.OnMouseDown(args); return;
            }
            if (pinStates[pinIndex] != PinStates.Locked)
            {
                pinStates[pinIndex] = PinStates.Down;
                pushingPin = true;
                sLoopInteract?.Start();
            }

            args.Handled = true;
            base.OnMouseDown(args);
        }

        public override void OnMouseUp(MouseEvent args)
        {
            if (args.Button != EnumMouseButton.Left) return;

            int i = pinIndex;
            bool lockedSet = false;

            if (pinStates[i] == PinStates.Down)
            {
                if (PinIsInPosition(i))
                {
                    bool canBindHere =
                        pinIsBinding != null &&
                        pinIsBinding[i] &&
                        !IsLastPlayablePin(i) &&
                        ExistsAnyNonBindingOtherThan(i);

                    if (canBindHere)
                    {
                        capi?.TriggerIngameError("thieverymod-pin", "binding",
                            Lang.Get("thievery:binding-warning", (pinIndex + 1)));
                        pinStates[i] = PinStates.Still;
                        pinPositions[i] = 0f;
                        lastClickPos[i] = -1;
                        UnbindAll();
                        RebindPins();
                        pushingPin = false;
                        sLoopInteract?.Stop();
                        args.Handled = true;
                        base.OnMouseUp(args);
                        return;
                    }
                    pinStates[i] = PinStates.Locked;
                    PlayOneShot(sSet);
                    lockedSet = true;
                    RebindPins();
                }
                else
                {
                    int codePos = CurrentPinCodePos(i);
                    bool atFalseHotspot = pinFalseSetCodes[i].Contains(codePos);

                    if (pinRotations[i] == 0.0) DamageLockpick();

                    TryRandomlyUnsetASetPin(atFalseHotspot);
                }
            }
            if (!lockedSet)
            {
                pinStates[i] = PinStates.Still;
                pinPositions[i] = 0f;
                lastClickPos[i] = -1;
            }

            pushingPin = false;
            sLoopInteract?.Stop();
            args.Handled = true;
            base.OnMouseUp(args);
        }

        public override void OnKeyDown(KeyEvent args)
        {
            if (args.KeyCode == (int)GlKeys.A)
            {
                if (ignoreLeftUntilUp) { args.Handled = true; base.OnKeyDown(args); return; }
                if (!leftHeld)
                {
                    leftHeld = true;
                    StepSelection(-1);
                    SingleComposer.GetCustomDraw("minigameDraw")?.Redraw();
                }
                args.Handled = true;
            }
            else if (args.KeyCode == (int)GlKeys.D)
            {
                if (ignoreRightUntilUp) { args.Handled = true; base.OnKeyDown(args); return; }

                if (!rightHeld)
                {
                    rightHeld = true;
                    StepSelection(+1);
                    SingleComposer.GetCustomDraw("minigameDraw")?.Redraw();
                }
                args.Handled = true;
            }

            base.OnKeyDown(args);
        }

        public override void OnKeyUp(KeyEvent args)
        {
            if (args.KeyCode == (int)GlKeys.A)
            {
                leftHeld = false;
                ignoreLeftUntilUp = false;
                args.Handled = true;
            }
            else if (args.KeyCode == (int)GlKeys.D)
            {
                rightHeld = false;
                ignoreRightUntilUp = false;
                args.Handled = true;
            }

            base.OnKeyUp(args);
        }
        
        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            guiOpenedTime = capi.ElapsedMilliseconds;

            var ctrls = capi.World.Player.Entity.Controls;

            ignoreLeftUntilUp  = ctrls.Left;
            ignoreRightUntilUp = ctrls.Right;

            leftHeld = rightHeld = false;

            ctrls.Left = ctrls.Right = ctrls.Forward = ctrls.Backward =
                ctrls.Jump = ctrls.Sprint = ctrls.Up = ctrls.Down = false;
            
            if (capi?.World == null || capi.World.Player?.Entity == null)
            {
                capi?.Logger.Error("[Lockpicking] World or player missing; closing.");
                TryClose();
                return;
            }

            string lockTypeCode = lockType ?? "game:padlock-iron";
            LoadAllSounds();
            ItemStack? pickStack = GetLockpickStackOrDefault();
            ItemSlot offhandSlot = capi.World.Player.Entity.LeftHandItemSlot;
            initialActiveSlot  = capi.World.Player.InventoryManager.ActiveHotbarSlot;
            initialOffhandSlot = capi.World.Player.Entity.LeftHandItemSlot;

            initialActiveCode  = initialActiveSlot?.Itemstack?.Collectible?.Code;
            initialOffhandCode = initialOffhandSlot?.Itemstack?.Collectible?.Code;

            sessionTokenActive  = Guid.NewGuid().ToString("N");
            sessionTokenOffhand = Guid.NewGuid().ToString("N");
            capi.Network.GetChannel("thievery").SendPacket(new StartLockpickSessionPacket {
                ActiveInventoryId   = initialActiveSlot?.Inventory?.InventoryID,
                ActiveSlotId        = initialActiveSlot?.Inventory?.GetSlotId(initialActiveSlot) ?? -1,
                OffhandInventoryId  = initialOffhandSlot?.Inventory?.InventoryID,
                OffhandSlotId       = initialOffhandSlot?.Inventory?.GetSlotId(initialOffhandSlot) ?? -1,
                ActiveToken         = sessionTokenActive,
                OffhandToken        = sessionTokenOffhand
            });
            tokenEnforcementActive = false;
            tokenEnforceStartMs = capi.ElapsedMilliseconds + tokenEnforceDelayMs;
            MultiTextureMeshRef? pickMesh = null;
            MultiTextureMeshRef? wrenchMesh = null;
            MultiTextureMeshRef? lockMesh = null;

            try
            {
                pickMesh = capi.TesselatorManager.GetDefaultItemMeshRef(pickStack?.Item);
                wrenchMesh = capi.TesselatorManager.GetDefaultItemMeshRef(offhandSlot?.Itemstack?.Item);
                lockMesh = capi.TesselatorManager.GetDefaultItemMeshRef(ResolveItem(capi, lockTypeCode));
            }
            catch (Exception e)
            {
                capi.Logger.Warning("[Lock UI] Exception creating cached meshes: " + e);
            }

            lockRenderer = new SingleMeshGuiRenderer(
                capi,
                lockMesh, ownsMesh: false,
                fallback: null,
                offsetX: -10f, offsetY: 916f, zLift: -1000, scale: 2500f,
                yawDeg: -158f, pitchDeg: 179f, rollDeg: 0f,
                pivot: new Vec3f(8f/16f, 1.625f/16f, 8f/16f),
                renderOrder: 0.92
            );
            lockRenderer.AmbientOverride = new Vec3f(1f, 1f, 1f);
            lockRenderer.SwayEnabled = true;
            lockRenderer.SwayAmpDeg = 3f;
            lockRenderer.SwaySpeed = 2f;
            lockRenderer.SwayAxisWeights = new Vec3f(0.6f, 0.25f, 0f);
            lockRenderer.SwayPhaseRad = 0f;

            wrenchRenderer = new SingleMeshGuiRenderer(
                capi,
                wrenchMesh, ownsMesh: false,
                fallback: offhandSlot,
                offsetX: 43f, offsetY: 801f, zLift: -937f, scale: 1411f,
                yawDeg: 211f, pitchDeg: 70f, rollDeg: 82f,
                pivot: new Vec3f(8f/16f, 0.75f/16f, 9.5f/16f),
                renderOrder: 0.90
            );
            wrenchRenderer.AmbientOverride = new Vec3f(0.8f, 0.8f, 0.8f);
            wrenchRenderer.SwayEnabled = true;
            wrenchRenderer.SwayAmpDeg = 3.0f;
            wrenchRenderer.SwaySpeed = 0.8f;
            wrenchRenderer.SwayAxisWeights = new Vec3f(0.3f, 0.9f, 0.1f);
            wrenchRenderer.SwayPhaseRad = 1.2f;
            pickRenderer = new SingleMeshGuiRenderer(
                capi,
                pickMesh, ownsMesh: false,
                fallback: capi.World.Player.InventoryManager.ActiveHotbarSlot,
                offsetX: -78f, offsetY: 690f, zLift: -1036f, scale: 1415f,
                yawDeg: -229f, pitchDeg: 360f, rollDeg: 2f,
                pivot: new Vec3f(8f/16f, 0.75f/16f, 9.5f/16f),
                renderOrder: 0.91
            );
            pickRenderer.AmbientOverride = new Vec3f(1.6f, 1.6f, 1.6f);
            pickRenderer.SwayEnabled = true;
            pickRenderer.SwayAmpDeg = 3.2f;
            pickRenderer.SwaySpeed = 1.0f;
            pickRenderer.SwayAxisWeights = new Vec3f(0.45f, 0.5f, 0f);
            pickRenderer.SwayPhaseRad = -0.9f;
            pickPitchBaseDeg = pickRenderer.PitchDeg;
            pickYawBaseDeg   = pickRenderer.YawDeg;
            pickRollBaseDeg  = pickRenderer.RollDeg;
            RecomputePickDepthTargets();
            SnapPickDepthToTargets();
            SnapPickTiltToCurrent();
            capi.Event.RegisterRenderer(wrenchRenderer, EnumRenderStage.Ortho, "thievery-lock-ui-wrench");
            capi.Event.RegisterRenderer(pickRenderer, EnumRenderStage.Ortho, "thievery-lock-ui-pick");
            capi.Event.RegisterRenderer(lockRenderer, EnumRenderStage.Ortho, "thievery-lock-ui-lock");

            capi.Input.InWorldAction += OnInWorldAction;
            capi.World.Player.Entity.Controls.Sneak = true;
            updateCallbackId = capi.Event.RegisterCallback(Update, 16);
        }
        private void RecomputePickDepthTargets()
        {
            if (pickRenderer == null) return;
            int enabledCount = 0;
            for (int i = 0; i < MaxPinCount; i++) if (!pinInitiallyDisabled[i]) enabledCount++;

            if (enabledCount <= 1)
            {
                pickTargetX = pickXBase;
                pickTargetZ = pickZBase;
                return;
            }
            int rank = 0, seen = 0;
            for (int i = 0; i < MaxPinCount; i++)
            {
                if (pinInitiallyDisabled[i]) continue;
                if (i == pinIndex) { rank = seen; break; }
                seen++;
            }

            float t = (float)rank / (enabledCount - 1);
            pickTargetX = pickXBase + t * pickXDelta;
            pickTargetZ = pickZBase + t * pickZDelta;
        }
        private void UpdatePickDepthLerped(float dt)
        {
            if (pickRenderer == null) return;
            float t = GameMath.Clamp(dt * pickDepthLerpPerSec, 0f, 1f);

            pickRenderer.OffX  = GameMath.Lerp(pickRenderer.OffX,  pickTargetX, t);
            pickRenderer.ZLift = GameMath.Lerp(pickRenderer.ZLift, pickTargetZ, t);
        }
        private float CurrentActivePush01()
        {
            if (pinInitiallyDisabled[pinIndex]) return 0f;
            if (pinStates[pinIndex] == PinStates.Locked) return 0f;
            return GameMath.Clamp(pinPositions[pinIndex], 0f, 1f);
        }
        private void SnapPickDepthToTargets()
        {
            if (pickRenderer == null) return;
            pickRenderer.OffX  = pickTargetX;
            pickRenderer.ZLift = pickTargetZ;
        }
        private void UpdatePickTilt(float dt)
        {
            if (pickRenderer == null) return;

            float push = CurrentActivePush01();
            float targetPitch = pickPitchBaseDeg + pickPitchMaxDelta * push;
            float targetYaw   = pickYawBaseDeg   + pickYawMaxDelta   * push;
            float targetRoll  = pickRollBaseDeg  + pickRollMaxDelta  * push;
            float tp = GameMath.Clamp(dt * pickTiltLerpPerSec, 0f, 1f);
            float ty = GameMath.Clamp(dt * pickYawLerpPerSec,  0f, 1f);
            float tr = GameMath.Clamp(dt * pickRollLerpPerSec, 0f, 1f);

            pickRenderer.PitchDeg = GameMath.Lerp(pickRenderer.PitchDeg, targetPitch, tp);
            pickRenderer.YawDeg   = GameMath.Lerp(pickRenderer.YawDeg,   targetYaw,   ty);
            pickRenderer.RollDeg  = GameMath.Lerp(pickRenderer.RollDeg,  targetRoll,  tr);
        }



        private void SnapPickTiltToCurrent()
        {
            if (pickRenderer == null) return;
            float push = CurrentActivePush01();

            pickRenderer.PitchDeg = pickPitchBaseDeg + pickPitchMaxDelta * push;
            pickRenderer.YawDeg   = pickYawBaseDeg   + pickYawMaxDelta   * push;
            pickRenderer.RollDeg  = pickRollBaseDeg  + pickRollMaxDelta  * push;
        }


        private MultiTextureMeshRef? CachedMeshForItem(ICoreClientAPI capi, Item? item)
        {
            if (item == null) return null;
            return capi.TesselatorManager.GetDefaultItemMeshRef(item);
        }

        private MultiTextureMeshRef? ManualMeshForItem(ICoreClientAPI capi, Item? item, out bool owns)
        {
            owns = false;
            if (item == null) return null;

            ITexPositionSource tex = capi.Tesselator.GetTextureSource(item, true);
            capi.Tesselator.TesselateItem(item, out var meshData, tex);
            if (meshData != null && meshData.VerticesCount > 0)
            {
                var mr = capi.Render.UploadMultiTextureMesh(meshData);
                owns = (mr != null);
                return mr;
            }
            return null;
        }

        private Item? ResolveItem(ICoreClientAPI capi, string codeOrFull)
        {
            var loc = AssetLocation.Create(codeOrFull);
            if (loc.Domain == null || loc.Domain == "") loc = new AssetLocation("game", loc.Path);
            return capi.World.GetItem(loc);
        }

        public override void OnGuiClosed()
        {
            capi.Input.InWorldAction -= OnInWorldAction;

            if (wrenchRenderer != null) { capi.Event.UnregisterRenderer(wrenchRenderer, EnumRenderStage.Ortho); wrenchRenderer.Dispose(); wrenchRenderer = null; }
            if (pickRenderer   != null) { capi.Event.UnregisterRenderer(pickRenderer,   EnumRenderStage.Ortho); pickRenderer.Dispose();   pickRenderer   = null; }
            if (lockRenderer   != null) { capi.Event.UnregisterRenderer(lockRenderer,   EnumRenderStage.Ortho); lockRenderer.Dispose();   lockRenderer   = null; }

            if (updateCallbackId != 0) { capi.Event.UnregisterCallback(updateCallbackId); updateCallbackId = 0; }
            StopAndDispose(sLoopInteract); sLoopInteract = null;
            StopAndDispose(sHotspot);      sHotspot      = null;
            StopAndDispose(sFalse);        sFalse        = null;
            StopAndDispose(sSet);          sSet          = null;
            StopAndDispose(sSelect);       sSelect       = null;
            if (initialActiveSlot != null || initialOffhandSlot != null)
            {
                try {
                    capi.Network.GetChannel("thievery").SendPacket(new EndLockpickSessionPacket {
                        ActiveInventoryId  = initialActiveSlot?.Inventory?.InventoryID,
                        ActiveSlotId       = initialActiveSlot?.Inventory?.GetSlotId(initialActiveSlot) ?? -1,
                        OffhandInventoryId = initialOffhandSlot?.Inventory?.InventoryID,
                        OffhandSlotId      = initialOffhandSlot?.Inventory?.GetSlotId(initialOffhandSlot) ?? -1
                    });
                } catch { /* ignore */ }
            }
            base.OnGuiClosed();
        }

        private ItemStack? GetLockpickStackOrDefault()
        {
            var slot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
            if (slot?.Itemstack?.Collectible is ItemLockpick)
            {
                return slot.Itemstack.Clone();
            }
            var loc  = new AssetLocation("thievery", $"lockpick-{DefaultMetal}");
            var item = capi.World.GetItem(loc);
            if (item == null)
            {
                capi.Logger.Error($"[Lockpicking] Could not find fallback item {loc}. Closing GUI.");
                return null;
            }
            return new ItemStack(item, 1);
        }

        private void OnInWorldAction(EnumEntityAction action, bool on, ref EnumHandling handled)
        {
            switch (action)
            {
                case EnumEntityAction.Forward:
                case EnumEntityAction.Backward:
                case EnumEntityAction.Left:
                case EnumEntityAction.Right:
                case EnumEntityAction.Jump:
                case EnumEntityAction.Sneak:
                case EnumEntityAction.Sprint:
                case EnumEntityAction.Glide:
                case EnumEntityAction.FloorSit:
                case EnumEntityAction.Up:
                case EnumEntityAction.Down:
                case EnumEntityAction.CtrlKey:
                case EnumEntityAction.ShiftKey:
                case EnumEntityAction.InWorldLeftMouseDown:
                case EnumEntityAction.InWorldRightMouseDown:
                    handled = EnumHandling.PreventDefault;
                    break;

                default:
                    return;
            }
            if (action == EnumEntityAction.Sneak && !on)
            {
                capi.World.Player.Entity.Controls.Sneak = true;
            }
            if (action == EnumEntityAction.InWorldRightMouseDown && on)
            {
                var now = capi.ElapsedMilliseconds;
                if (now - guiOpenedTime > rightClickDelay)
                {
                    TryClose();
                }
            }
        }
        private void TryRandomlyUnsetASetPin(bool wasFalseHotspot)
        {
            double p = wasFalseHotspot ? falseUnsetChance : missUnsetChance;
            if (capi.World.Rand.NextDouble() >= p) return;
            List<int> candidates = new List<int>();
            for (int k = 0; k < MaxPinCount; k++)
            {
                if (!pinInitiallyDisabled[k] && pinStates[k] == PinStates.Locked)
                    candidates.Add(k);
            }
            if (candidates.Count == 0) return;

            int pick = candidates[capi.World.Rand.Next(candidates.Count)];
            pinStates[pick] = PinStates.Still;
            pinPositions[pick] = 0f;
            lastClickPos[pick] = -1;
            PlayOneShot(wasFalseHotspot ? sFalse : sSet);
            capi?.TriggerIngameError("thieverymod-pin", "unset",
                Lang.Get("thievery:unset-notice", (pick + 1)));
        }

        private void CheckForRequiredItems()
        {
            var p = capi.World?.Player;
            if (p == null) return;

            var curActiveSlot = p.InventoryManager.ActiveHotbarSlot;
            var curOffhandSlot = p.Entity.LeftHandItemSlot;
            if (!tokenEnforcementActive)
            {
                bool bothTokensVisible =
                    curActiveSlot?.Itemstack?.Attributes?.GetString(SessionAttrKey) == sessionTokenActive &&
                    curOffhandSlot?.Itemstack?.Attributes?.GetString(SessionAttrKey) == sessionTokenOffhand;

                if (bothTokensVisible || capi.ElapsedMilliseconds >= tokenEnforceStartMs)
                {
                    tokenEnforcementActive = true;
                }
                else
                {
                    return;
                }
            }
            bool activeSlotChanged = !object.ReferenceEquals(curActiveSlot, initialActiveSlot);
            bool offhandSlotChanged = !object.ReferenceEquals(curOffhandSlot, initialOffhandSlot);

            bool activeMissingOrCodeChanged =
                curActiveSlot?.Itemstack == null ||
                initialActiveCode == null ||
                !curActiveSlot.Itemstack.Collectible.Code.Equals(initialActiveCode);

            bool offhandMissingOrCodeChanged =
                curOffhandSlot?.Itemstack == null ||
                initialOffhandCode == null ||
                !curOffhandSlot.Itemstack.Collectible.Code.Equals(initialOffhandCode);

            bool activeTokenMismatch =
                curActiveSlot?.Itemstack?.Attributes?.GetString(SessionAttrKey) != sessionTokenActive;

            bool offhandTokenMismatch =
                curOffhandSlot?.Itemstack?.Attributes?.GetString(SessionAttrKey) != sessionTokenOffhand;

            if (activeSlotChanged || offhandSlotChanged ||
                activeMissingOrCodeChanged || offhandMissingOrCodeChanged ||
                activeTokenMismatch || offhandTokenMismatch)
            {
                capi.TriggerIngameError("thievery-lockui", "items-changed",
                    Lang.Get("thievery:items-changed-cancel"));
                TryClose();
            }
        }

        private void DamageTensionWrench(float difficultyMultiplier = 1f)
        {
            ICoreClientAPI client = capi;
            EntityAgent entityAgent = client.World.Player.Entity as EntityAgent;
            if (entityAgent == null) return;

            var offhandSlot = entityAgent.LeftHandItemSlot;
            if (offhandSlot != null && offhandSlot.Itemstack != null)
            {
                string itemCode = offhandSlot.Itemstack.Collectible.Code.Path;
                if (itemCode.StartsWith("tensionwrench-"))
                {
                    int damageAmount = (int)(ModConfig.Instance.MiniGame.TensionWrenchDamageBase * difficultyMultiplier/2);

                    client.Network.GetChannel("thievery").SendPacket(new ItemDamagePacket
                    {
                        InventoryId = offhandSlot.Inventory.InventoryID,
                        SlotId = offhandSlot.Inventory.GetSlotId(offhandSlot),
                        Damage = damageAmount
                    });
                }
            }
        }

        private void DamageLockpick(float difficultyMultiplier = 1f)
        {
            ICoreClientAPI client = capi;
            var activeSlot = client.World.Player.InventoryManager.ActiveHotbarSlot;
            if (activeSlot != null && activeSlot.Itemstack != null)
            {
                string itemCode = activeSlot.Itemstack.Collectible.Code.Path;
                if (itemCode.StartsWith("lockpick-"))
                {
                    int damageAmount = (int)(ModConfig.Instance.MiniGame.LockpickDamageBase * difficultyMultiplier);

                    client.Network.GetChannel("thievery").SendPacket(new ItemDamagePacket
                    {
                        InventoryId = activeSlot.Inventory.InventoryID,
                        SlotId = activeSlot.Inventory.GetSlotId(activeSlot),
                        Damage = damageAmount
                    });
                }
            }
        }

        
        private ILoadedSound LoadSound(string pathNoExt, bool loop, float volume = 1f, float pitch = 1f)
        {
            var pos = new Vec3f((float)bePos.X + 0.5f, (float)bePos.Y + 0.5f, (float)bePos.Z + 0.5f);
            return capi.World.LoadSound(new SoundParams
            {
                Location = new AssetLocation("thievery", pathNoExt),
                Position = pos,
                DisposeOnFinish = false,
                Pitch = pitch,
                Volume = volume,
                Range = 16f,
                ShouldLoop = loop
            });
        }

        private void LoadAllSounds()
        {
            sLoopInteract = LoadSound("sounds/lockpicking",        loop: true,  volume: 0.1f);
            sHotspot      = LoadSound("sounds/lockpicking_hotspot",loop: false, volume: 0.9f);
            sFalse        = LoadSound("sounds/lockpicking_false",  loop: false, volume: 0.9f);
            sSet          = LoadSound("sounds/lockpicking_set",    loop: false, volume: 0.9f);
            sSelect       = LoadSound("sounds/lockpicking_select", loop: false, volume: 0.5f);
        }

        private void StopAndDispose(ILoadedSound snd)
        {
            try { snd?.Stop(); } catch { }
            try { snd?.Dispose(); } catch { }
        }

        private void PlayOneShot(ILoadedSound snd)
        {
            try { snd?.Start(); } catch { }
        }
        
    }
}