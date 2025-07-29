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

namespace Thievery.src.LockpickAndTensionWrench
{
    public class GuiLockpickingMiniGameESStyle : GuiDialogBlockEntity
    {
        public event Action OnComplete;

        private const int MaxPinCount = 5;
        private const int MaxPinCode = 4; // 0-4

        private ICoreClientAPI capi;
        private int difficultyLevel;
        private bool bindingOrder = false;
        private string lockType;

        private bool running = true;

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
            // Default is a nice bronzey colour
            return new Color(0.671, 0.514, 0.039);
        }

        public GuiLockpickingMiniGameESStyle(
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

            for (int i = 0; i < MaxPinCount; ++i)
            {
                pinStates[i] = i < pins ? PinStates.Still : PinStates.Locked;
                pinCodes[i] = i < pins ? capi.World.Rand.Next(1, MaxPinCode + 1) : MaxPinCode;
            }

            if (bindingOrder)
            {
                pinBindingOrder = Enumerable.Range(0, MaxPinCount).ToArray();
                GameMath.Shuffle(Random.Shared, pinBindingOrder);
            }
        }

        private int CalculatePinCount()
        {
            return this.difficultyLevel switch
            {
                <= 25 => 3,
                <= 75 => 4,
                _ => MaxPinCount
            };
        }

        private int pinIndex = 0;

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

            for (int i = 0; i < MaxPinCount; ++i)
            {
                if (pinStates[i] == PinStates.Locked)
                {
                    pinPositions[i] = (float)pinCodes[i] / (float)MaxPinCode;
                }
            }
        }

        private float lastUpdateMs;
        private void OnMinigameDraw(Context ctx, ImageSurface surface, ElementBounds elementBounds)
        {
            ctx.Save();
            long currentMs = capi.ElapsedMilliseconds;
            float deltaTime = (currentMs - lastUpdateMs) / 1000f;
            lastUpdateMs = currentMs;
            float timeSec = currentMs / 1000f;
            
            DrawLock(ctx,timeSec);
            ctx.Restore();
        }

        private const int pinPadding = 32;

        private void DrawLock(Context ctx, float timeNow)
        {
            ctx.Translate( drawingX,drawingY);
            if(false /*Lock texture Surface */)
            {

            } else
            {
                using var bgPat = new RadialGradient(
                    drawingX + lockWidth / 2, drawingY + lockHeight / 2,
                    lockWidth * 0.1,
                    drawingX + lockWidth / 2, drawingY + lockHeight / 2,
                    lockWidth / 2);
                Color lockColor = GetLockColor();
                Color darkerColor = new Color(lockColor.R * 0.5, lockColor.G * 0.5, lockColor.B * 0.5);
                bgPat.AddColorStop(0,lockColor);
                bgPat.AddColorStop(1, darkerColor);
                ctx.SetSource(bgPat);
                ctx.Rectangle(0, 0, lockWidth, lockHeight);
                ctx.Fill();
            }

            DrawPins(ctx,timeNow);
            DrawLockpick(ctx);
            DrawTimer(ctx);
        }

        private float[] pinPositions = new float[MaxPinCount];
        private float[] pinRotations = new float[MaxPinCount];
        private float pinWidth = 32;
        private float pinHeight = 128;

        private int CurrentPinCodePos(int index) => (int)Math.Round(pinPositions[index] * (MaxPinCode + 1), 0);

        private bool PinIsInPosition(int index)
        {
            return CurrentPinCodePos(index) == pinCodes[index] &&
                   pinPositions[index] > 0;
        }

        private void DrawPins(Context ctx, float timeSec)
        {
            //Rectangles (for now)

            var pinAreaWidth = (lockWidth - pinPadding * 2);
            var pinAreaStart = lockHeight * 0.5;
            var pinAreaEnd = lockHeight - pinHeight - pinPadding;
            var pinAreaHeight = pinAreaEnd - pinAreaStart;

            for (int i = 0; i < pinCodes.Length; ++i)
            {
                var code = pinCodes[i];
                var pinPosition = pinPositions[i];
                if (pinStates[i] == PinStates.Down && PinIsInPosition(i))
                {
                    pinRotations[i] = (float)Math.Sin((timeSec * 50.0)) * 0.02f;
                }
                else
                {
                    pinRotations[i] = 0;
                }
            }

            for (int i = 0; i < pinPositions.Length; ++i)
            {
                var x = pinPadding + (pinAreaWidth / pinPositions.Length) * i;
                var y = pinAreaStart + (pinAreaHeight) * pinPositions[i]; 
                ctx.Save();
                ctx.NewPath();
                if (pinStates[i] == PinStates.Locked)
                {
                    ctx.SetSourceRGB(0.3, 0.3, 0.3);
                }
                else if (pinStates[i] == PinStates.Down)
                {
                    ctx.SetSourceRGB(0.8, 0.8, 0.8);
                }
                else
                {
                    ctx.SetSourceRGBA(0.8,0.8,0.8,0.5);
                }
                ctx.Translate(x + pinWidth / 2, y);
                ctx.Rotate(pinRotations[i]);
                ctx.Rectangle(0, 0, pinWidth, pinHeight);
                ctx.TextPath($"{CurrentPinCodePos(i)} {
                    pinStates[i] switch { PinStates.Still => "S", PinStates.Down => "D", PinStates.Up => "U", PinStates.Locked => "L", }} {
                        (i < pinCodes.Length ? pinCodes[i] : "N")}");
                ctx.Fill();
                if (this.pinIndex == i)
                {
                    //Draw highlight
                    ctx.SetSourceRGB(0.75, 0.75, 1);
                    ctx.Rectangle(0,0,pinWidth,pinHeight);
                    ctx.LineWidth = 5.0;
                    ctx.Stroke();
                }
                ctx.Restore();
                DrawSpring(ctx, y + pinHeight, x + pinWidth/2);
            }
        }

        private void DrawSpring(Context ctx, double pinBottom, double pinX)
        {
            const int springLines = 8;
            const double bottomOfLock = lockHeight - pinPadding;
            //Just for now
            ctx.Save();
            ctx.NewPath();
            var leftX = pinX - 2;
            var rightX = pinX + pinWidth + 2;
            ctx.SetSourceRGB(0, 1, 0);
            ctx.MoveTo(rightX,pinBottom);
            ctx.LineTo(leftX, pinBottom);
            var totalAreaToCover = bottomOfLock - pinBottom;
            var left = false;
            for (int i = 1; i < springLines; ++i)
            {
                var y = pinBottom + (totalAreaToCover / springLines) * i;
                var x = left ? leftX : rightX;
                ctx.LineTo(x,y);
                left = !left;
            }
            ctx.LineTo(left ? leftX : rightX, bottomOfLock);
            left = !left;
            ctx.LineTo(left ? leftX : rightX, bottomOfLock);
            ctx.LineWidth = 1.0;
            ctx.Stroke();
            ctx.Restore();
        }

        private void DrawLockpick(Context ctx)
        {
            //Draw the lockpick
            ctx.Save();
            ctx.NewPath();
            ctx.SetSourceRGB(0.8, 0.8, 0.8);
            ctx.MoveTo(0,64);
            ctx.LineTo(160, 64);
            ctx.LineTo(154, 70);
            ctx.LineTo(148, 64);
            ctx.Stroke();
            ctx.Restore();
        }

        private void DrawTimer(Context ctx)
        {
            
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
                DamageTensionWrench((int)damage * this.difficultyLevel);
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
                }
                else if (pinStates[i] == PinStates.Up)
                {
                    pinPositions[i] = (float)Math.Clamp(pinPositions[i] - pinRiseSpeed, 0, 1.0);
                }
                
                if (pinStates[i] == PinStates.Up && pinPositions[i] <= 0)
                {
                    pinStates[i] = PinStates.Still;
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

        public override bool CaptureRawMouse() => false;
        public override bool ShouldReceiveMouseEvents() => true;

        private bool pushingPin = false;
        private const float pinPushSpeed = 0.01f;
        private const float pinRiseSpeed = 0.015f;
        private float pickPosition = 0.0f;
        public override void OnMouseMove(MouseEvent args)
        {
            //Determine which pin over
            var customDraw = SingleComposer.GetCustomDraw("minigameDraw");

            var relativePosition = (args.X - customDraw.Bounds.absX - drawingX)/lockWidth;
            if (!pushingPin)
            {
                pickPosition = (float)Math.Clamp(relativePosition, 0, 0.9999f);
                this.pinIndex = (int)Math.Clamp(Math.Floor(pickPosition * MaxPinCount), 0, MaxPinCount - 1);
            }
            base.OnMouseMove(args);
        }

        public override void OnMouseDown(MouseEvent args)
        {
            //Lower pick IF over pin
            if(args.Button != EnumMouseButton.Left)
            {
                return;
            }

            if (pinStates[pinIndex] != PinStates.Locked)
            {
                pinStates[pinIndex] = PinStates.Down;
                pushingPin = true;
            }

            args.Handled = true;
            base.OnMouseDown(args);
        }

        public override void OnMouseUp(MouseEvent args)
        {
            //Release the pick
            if (args.Button != EnumMouseButton.Left)
            {
                return;
            }

            if (pinStates[pinIndex] == PinStates.Down && PinIsInPosition(pinIndex))
            {
                pinStates[pinIndex] = PinStates.Locked;
            } else if (pinStates[pinIndex] == PinStates.Down && pinRotations[pinIndex] == 0.0)
            {
                pinStates[pinIndex] = PinStates.Up;
                DamageLockpick();
            }

            if (pinStates[pinIndex] == PinStates.Down)
            {
                pinStates[pinIndex] = PinStates.Up;
            }

            pushingPin = false;
            args.Handled = true;
            base.OnMouseUp(args);
        }

        public override void OnGuiOpened()
        {
            capi.Event.RegisterCallback(Update, 16);
            base.OnGuiOpened();
        }

        public override void OnGuiClosed()
        {
            running = false;
            capi.Event.UnregisterCallback(updateCallbackId);
            updateCallbackId = 0;
            base.OnGuiClosed();
        }

        private void CheckForRequiredItems()
        {

        }

        private void DamageTensionWrench(int damageAmount = 5)
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
                    client.Network.GetChannel("thievery").SendPacket(new ItemDamagePacket
                    {
                        InventoryId = offhandSlot.Inventory.InventoryID,
                        SlotId = offhandSlot.Inventory.GetSlotId(offhandSlot),
                        Damage = damageAmount
                    });
                }
            }
        }

        private void DamageLockpick()
        {
            ICoreClientAPI client = capi;
            var activeSlot = client.World.Player.InventoryManager.ActiveHotbarSlot;
            if (activeSlot != null && activeSlot.Itemstack != null)
            {
                string itemCode = activeSlot.Itemstack.Collectible.Code.Path;
                if (itemCode.StartsWith("lockpick-"))
                {
                    int damageAmount = 50;
                    client.Network.GetChannel("thievery").SendPacket(new ItemDamagePacket
                    {
                        InventoryId = activeSlot.Inventory.InventoryID,
                        SlotId = activeSlot.Inventory.GetSlotId(activeSlot),
                        Damage = damageAmount
                    });
                }
            }
        }
    }
}
