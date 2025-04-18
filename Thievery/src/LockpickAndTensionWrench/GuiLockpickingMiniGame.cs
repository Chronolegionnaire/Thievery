using System;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using System.Collections.Generic;
using System.IO;
using Thievery.LockpickAndTensionWrench;

public class GuiLockpickingMinigame : GuiDialogBlockEntity
{
    public event Action OnComplete;

    private ICoreClientAPI capi;
    private int numBoxes;
    private float[] boxFills;
    private bool[] boxLocked;
    private bool[] boxCycleLocked;
    private float[] cycleLockTimers;
    private float[] nextCycleInterval;
    private float minCycleInterval = 2.0f;
    private float maxCycleInterval = 5.0f;
    private float cycleActiveDuration = 1.5f;
    private float pointerSpeed = 0.25f;
    private float pointerPosition = 0f;
    private float hotspotLogicalCenter = 0.5f;
    private float hotspotDisplayCenter = 0.5f;
    private float hotspotWidth = 0.2f;
    private float hotspotVelocity = 0.0f;
    private float hotspotDirectionTimer = 0f;
    private float hotspotDirectionIntervalMin = 1f;
    private float hotspotDirectionIntervalMax = 2f;
    private float hotspotMaxSpeed = 0.3f;
    private float keyPressSpeedMultiplier = 1.0f;
    private float hotspotTargetCenter;
    private float hotspotLerpSpeed = 20.0f;
    private float fillRateCorrect = 0.85f;
    private float rapidUnfillRate = 1.0f;
    private float slowUnfillRate = 0.03f;
    private bool userInputActive = false;
    private bool isMouseDown = false;
    private int? selectedBoxIndex = null;
    private long lastUpdateMs;
    private double drawingX = 10, drawingY = 40, drawingWidth = 300, drawingHeight = 260;
    private long updateCallbackId;
    private bool running = false;
    private float padlockDuration;
    private List<float> boxCenters = new List<float>();
    private long guiOpenedTime;
    private const long rightClickDelay = 200;
    private const int MaxBoxesPerRow = 4;
    private string lockType;
    private (double, double)[] tumblerCropOffsets;
    private float tensionWrenchDamageTimer = 0f;
    private float lockpickDamageTimer = 0f;
    private float damageThreshold = 0.5f;
    private bool spacebarHeld = false;
    private float spaceHeldTimer = 0f;
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
        return new Color(0.7, 0.7, 0.7);
    }
    public GuiLockpickingMinigame(
        string dialogTitle,
        BlockPos blockEntityPos,
        ICoreClientAPI capi,
        int padlockDurationMs,
        string lockType
    ) : base(dialogTitle, blockEntityPos, capi)
    {
        this.capi = capi;
        this.padlockDuration = padlockDurationMs;
        this.lockType = lockType;
        this.numBoxes = CalculateNumBoxes(padlockDurationMs);

        boxFills = new float[numBoxes];
        boxLocked = new bool[numBoxes];
        boxCycleLocked = new bool[numBoxes];
        cycleLockTimers = new float[numBoxes];

        nextCycleInterval = new float[numBoxes];
        Random rnd = new Random();
        for (int i = 0; i < numBoxes; i++)
        {
            nextCycleInterval[i] = (float)(rnd.NextDouble() * (maxCycleInterval - minCycleInterval) + minCycleInterval);
        }

        lastUpdateMs = capi.ElapsedMilliseconds;

        SetupDifficultyScaling();
        SetupDialog();
        BuildBoxCenters();
    }

    private int CalculateNumBoxes(int padlockDurationMs)
    {
        int minBoxes = 3;
        int maxBoxes = 8;
        int minDuration = 10000;
        int maxDuration = 360000;

        if (padlockDurationMs <= minDuration) return minBoxes;
        if (padlockDurationMs >= maxDuration) return maxBoxes;

        double t = (padlockDurationMs - minDuration) / (double)(maxDuration - minDuration);
        return (int)Math.Round(minBoxes + t * (maxBoxes - minBoxes));
    }

    private void SetupDifficultyScaling()
    {
        float difficulty = (padlockDuration - 10000f) / (360000f - 10000f);
        difficulty = Math.Max(0f, Math.Min(1f, difficulty));

        float minPtrSpeed = 0.2f;
        float maxPtrSpeed = 0.5f;
        pointerSpeed = minPtrSpeed + (maxPtrSpeed - minPtrSpeed) * difficulty;

        float maxHotspotWidth = 0.4f;
        float minHotspotWidth = 0.1f;
        hotspotWidth = maxHotspotWidth - difficulty * (maxHotspotWidth - minHotspotWidth);

        hotspotMaxSpeed = 2f + 2f * difficulty;
        hotspotDirectionIntervalMin = 0.5f;
        hotspotDirectionIntervalMax = 1.0f;

        Random rnd = new Random();
        hotspotLogicalCenter = (float)rnd.NextDouble() * (1f - hotspotWidth) + hotspotWidth / 2f;
        hotspotTargetCenter = hotspotLogicalCenter;
        hotspotVelocity = (float)(rnd.NextDouble() * 2 - 1) * hotspotMaxSpeed;
        hotspotDirectionTimer = (float)(rnd.NextDouble() * (hotspotDirectionIntervalMax - hotspotDirectionIntervalMin) + hotspotDirectionIntervalMin);
    }

    private void SetupDialog()
    {
        ElementBounds dialogBounds = ElementBounds.Fixed(0, 0, 320, 320)
            .WithAlignment(EnumDialogArea.CenterMiddle);
        ElementBounds drawBounds = ElementBounds.Fixed(drawingX, drawingY, drawingWidth, drawingHeight)
            .WithAlignment(EnumDialogArea.CenterMiddle);

        ClearComposers();

        SingleComposer = capi.Gui
            .CreateCompo("lockpickingminigame", dialogBounds)
            .AddDynamicCustomDraw(drawBounds, new DrawDelegateWithBounds(OnMinigameDraw), "minigameDraw")
            .Compose(true);
    }

    private void OnTitleBarClose()
    {
        TryClose();
    }
    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        guiOpenedTime = capi.ElapsedMilliseconds;
        running = true;
        capi.Input.InWorldAction += OnInWorldAction;
        capi.Input.InWorldAction += OnSneakAction;
        updateCallbackId = capi.Event.RegisterCallback(Update, 16);
        capi.World.Player.Entity.Controls.Sneak = true;
        string metal = lockType.Replace("padlock-", "");
        AssetLocation texLoc = new AssetLocation("game", $"textures/block/metal/ingot/{metal}.png");
        IAsset asset = capi.Assets.TryGet(texLoc);
        if (asset != null && asset.Data != null)
        {
            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{texLoc.Domain}_{texLoc.Path.Replace("/", "_")}.png");
            File.WriteAllBytes(tempFile, asset.Data);
            lockTextureSurface = new Cairo.ImageSurface(tempFile);
            tumblerCropOffsets = new (double, double)[numBoxes];
            Random rnd = new Random();
            double textureWidth = lockTextureSurface.Width;
            double textureHeight = lockTextureSurface.Height;
            double cropSize = 16.0;
            double maxOffsetX = textureWidth - cropSize;
            double maxOffsetY = textureHeight - cropSize;
            for (int i = 0; i < numBoxes; i++)
            {
                double offsetX = rnd.NextDouble() * maxOffsetX;
                double offsetY = rnd.NextDouble() * maxOffsetY;
                tumblerCropOffsets[i] = (offsetX, offsetY);
            }
        }
    }

    private void OnSneakAction(EnumEntityAction action, bool on, ref EnumHandling handled)
    {
        if (action == EnumEntityAction.Sneak && on == false)
        {
            handled = EnumHandling.PreventDefault;
            capi.World.Player.Entity.Controls.Sneak = true;
        }
    }

    private void OnInWorldAction(EnumEntityAction action, bool on, ref EnumHandling handled)
    {
        if (action == EnumEntityAction.RightMouseDown && on)
        {
            long currentTime = capi.ElapsedMilliseconds;
            if (currentTime - guiOpenedTime > rightClickDelay)
            {
                TryClose();
            }
            handled = EnumHandling.PreventDefault;
        }
    }

    public override void OnGuiClosed()
    {
        capi.Input.InWorldAction -= OnInWorldAction;
        capi.Input.InWorldAction -= OnSneakAction;
        running = false;
        capi.Event.UnregisterCallback(updateCallbackId);
        updateCallbackId = 0;
        capi.World.Player.Entity.Controls.RightMouseDown = false;
        base.OnGuiClosed();
    }

    public override void OnKeyDown(KeyEvent args)
    {
        int key = args.KeyCode;
        if (key == (int)GlKeys.A || key == (int)GlKeys.Left ||
            key == (int)GlKeys.D || key == (int)GlKeys.Right)
        {
            DamageTensionWrench();
            float nudgeAmount = 0.1f * keyPressSpeedMultiplier;
            float halfWidth = hotspotWidth / 2;
            if (key == (int)GlKeys.A || key == (int)GlKeys.Left)
            {
                hotspotTargetCenter -= nudgeAmount;
                userInputActive = true;
                args.Handled = true;
            }
            else if (key == (int)GlKeys.D || key == (int)GlKeys.Right)
            {
                hotspotTargetCenter += nudgeAmount;
                userInputActive = true;
                args.Handled = true;
            }
            hotspotTargetCenter = GameMath.Clamp(hotspotTargetCenter, halfWidth, 1f - halfWidth);
        }
        if (key == (int)GlKeys.Space)
        {
            spacebarHeld = true;
            hotspotTargetCenter = hotspotLogicalCenter;
            args.Handled = true;
        }
        base.OnKeyDown(args);
    }
    private void DamageTensionWrench()
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
                int damageAmount = 5;
                client.Network.GetChannel("thievery").SendPacket(new ItemDamagePacket
                {
                    InventoryId = offhandSlot.Inventory.InventoryID,
                    SlotId = offhandSlot.Inventory.GetSlotId(offhandSlot),
                    Damage = damageAmount
                });
            }
        }
    }

    public override void OnKeyUp(KeyEvent args)
    {
        if (args.KeyCode == (int)GlKeys.Space)
        {
            spacebarHeld = false;
            args.Handled = true;
        }
        userInputActive = false;
        base.OnKeyUp(args);
    }
    public override void OnMouseDown(MouseEvent args)
    {
        if (args.Button != EnumMouseButton.Left)
            return;
        var customDraw = SingleComposer.GetCustomDraw("minigameDraw");
        if (args.Button == EnumMouseButton.Left && customDraw != null)
        {
            ElementBounds bounds = customDraw.Bounds;
            double localX = args.X - bounds.absX;
            double localY = args.Y - bounds.absY;
            DamageLockpick();

            double boxSize = 40;
            double rowGap = 10;
            int totalRows = (int)Math.Ceiling(numBoxes / (double)MaxBoxesPerRow);
            double areaWidth = bounds.fixedWidth;
            double areaX = bounds.fixedX;
            double startY = bounds.fixedY + 60;
            double barY = startY + boxSize + 10;
            double meterHeight = 20;
            double originalRow1Y = startY + (boxSize + rowGap);
            double desiredRow1Y = barY + meterHeight + 10;
            double shift = desiredRow1Y - originalRow1Y;

            int boxIndex = 0;
            for (int row = 0; row < totalRows && boxIndex < numBoxes; row++)
            {
                int boxesInRow = (row == totalRows - 1)
                    ? numBoxes - row * MaxBoxesPerRow
                    : MaxBoxesPerRow;

                double spacing = (areaWidth - (boxesInRow * boxSize)) / (boxesInRow + 1);
                double rowY = startY + row * (boxSize + rowGap);
                if (row >= 1)
                    rowY += shift;

                for (int col = 0; col < boxesInRow && boxIndex < numBoxes; col++, boxIndex++)
                {
                    double boxX = areaX + spacing + col * (boxSize + spacing);
                    if (localX >= boxX && localX <= boxX + boxSize && localY >= rowY && localY <= rowY + boxSize)
                    {
                        if (!boxLocked[boxIndex] && !boxCycleLocked[boxIndex])
                        {
                            selectedBoxIndex = boxIndex;
                            isMouseDown = true;
                            args.Handled = true;
                            return;
                        }
                    }
                }
            }
        }
        args.Handled = selectedBoxIndex.HasValue;
        base.OnMouseDown(args);
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
                int damageAmount = 20;
                client.Network.GetChannel("thievery").SendPacket(new ItemDamagePacket
                {
                    InventoryId = activeSlot.Inventory.InventoryID,
                    SlotId = activeSlot.Inventory.GetSlotId(activeSlot),
                    Damage = damageAmount
                });
            }
        }
    }

    public override void OnMouseUp(MouseEvent args)
    {
        if (args.Button != EnumMouseButton.Left)
            return;
    
        selectedBoxIndex = null;
        isMouseDown = false;
        args.Handled = true;
        base.OnMouseUp(args);
    }

    public override bool CaptureRawMouse() => false;
    public override bool ShouldReceiveMouseEvents() => true; 
    private void Update(float dt)
    {
        if (!IsOpened() || !running) return;
        if (capi.World.Player != null)
        {
            capi.World.Player.Entity.Controls.Sneak = true;
            CheckForRequiredItems();
        }
        if (userInputActive)
        {
            tensionWrenchDamageTimer += dt;
            if (tensionWrenchDamageTimer >= damageThreshold)
            {
                DamageTensionWrench();
                tensionWrenchDamageTimer = 0f;
            }
        }
        else
        {
            tensionWrenchDamageTimer = 0f;
        }
        bool damagingLockpick = isMouseDown || spacebarHeld;
        if (damagingLockpick)
        {
            float currentThreshold = spacebarHeld ? damageThreshold * 0.5f : damageThreshold;
            lockpickDamageTimer += dt;
            if (lockpickDamageTimer >= currentThreshold)
            {
                DamageTensionWrench();
                DamageLockpick();
                lockpickDamageTimer = 0f;
            }
        }
        else
        {
            lockpickDamageTimer = 0f;
        }
        for (int i = 0; i < numBoxes; i++)
        {
            if (boxLocked[i]) continue;
            if (boxCycleLocked[i])
            {
                cycleLockTimers[i] -= dt;
                if (cycleLockTimers[i] <= 0)
                {
                    boxCycleLocked[i] = false;
                    nextCycleInterval[i] = NextRandomInterval();
                }
            }
            else
            {
                nextCycleInterval[i] -= dt;
                if (nextCycleInterval[i] <= 0)
                {
                    boxCycleLocked[i] = true;
                    cycleLockTimers[i] = cycleActiveDuration;
                }
            }
        }
        UpdateHotspot(dt);
        var customDraw = SingleComposer.GetCustomDraw("minigameDraw");
        customDraw?.Redraw();

        updateCallbackId = capi.Event.RegisterCallback(Update, 16);
    }
    private void CheckForRequiredItems()
    {
        var activeSlot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
        bool hasLockpick = activeSlot != null 
                           && activeSlot.Itemstack != null 
                           && activeSlot.Itemstack.Collectible != null 
                           && activeSlot.Itemstack.Collectible.Code.Path.StartsWith("lockpick-");
        EntityAgent entityAgent = capi.World.Player.Entity as EntityAgent;
        bool hasTensionWrench = false;
        if (entityAgent != null)
        {
            var offhandSlot = entityAgent.LeftHandItemSlot;
            hasTensionWrench = offhandSlot != null 
                               && offhandSlot.Itemstack != null 
                               && offhandSlot.Itemstack.Collectible != null 
                               && offhandSlot.Itemstack.Collectible.Code.Path.StartsWith("tensionwrench-");
        }
        if (!hasLockpick || !hasTensionWrench)
        {
            TryClose();
        }
    }
    private float NextRandomInterval()
    {
        Random rnd = new Random();
        return (float)(rnd.NextDouble() * (maxCycleInterval - minCycleInterval) + minCycleInterval);
    }

    private void UpdateHotspot(float dt)
    {
        float halfWidth = hotspotWidth * 0.5f;
        if (spacebarHeld)
        {
            spaceHeldTimer += dt;
            float shakeAmplitude = 0.02f;
            float shakeFrequency = 20f;
            float shake = shakeAmplitude * (float)Math.Sin(2 * Math.PI * shakeFrequency * spaceHeldTimer);
            hotspotLogicalCenter = hotspotTargetCenter + shake;
        }
        else if (userInputActive)
        {
            hotspotLogicalCenter = hotspotTargetCenter;
        }
        else
        {
            spaceHeldTimer = 0f;
            hotspotDirectionTimer -= dt;
            if (hotspotDirectionTimer <= 0)
            {
                Random rnd = new Random();
                float newSpeed = (float)(rnd.NextDouble() * 2 - 1) * hotspotMaxSpeed;
                hotspotVelocity = newSpeed;
                hotspotDirectionTimer = (float)(rnd.NextDouble() * (hotspotDirectionIntervalMax - hotspotDirectionIntervalMin) + hotspotDirectionIntervalMin);
            }
            hotspotLogicalCenter += hotspotVelocity * dt;
            if (hotspotLogicalCenter <= halfWidth)
            {
                hotspotLogicalCenter = halfWidth;
                hotspotVelocity = -hotspotVelocity;
            }
            else if (hotspotLogicalCenter >= 1f - halfWidth)
            {
                hotspotLogicalCenter = 1f - halfWidth;
                hotspotVelocity = -hotspotVelocity;
            }
        }
        hotspotDisplayCenter = GameMath.Lerp(hotspotDisplayCenter, hotspotLogicalCenter, Math.Min(1f, hotspotLerpSpeed * dt));
    }

    private void BuildBoxCenters()
    {
        boxCenters.Clear();
        int totalRows = (int)Math.Ceiling(numBoxes / (double)MaxBoxesPerRow);
        double boxSize = 40.0;
        int index = 0;
        for (int row = 0; row < totalRows; row++)
        {
            int boxesInRow = (row == totalRows - 1) ? numBoxes - row * MaxBoxesPerRow : MaxBoxesPerRow;
            for (int col = 0; col < boxesInRow && index < numBoxes; col++, index++)
            {
                float centerX = (float)((col + 0.5) / boxesInRow);
                boxCenters.Add(centerX);
            }
        }
    }

    private void OnMinigameDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
    {
        ctx.Save();

        long currentMs = capi.ElapsedMilliseconds;
        float deltaTime = (currentMs - lastUpdateMs) / 1000f;
        lastUpdateMs = currentMs;
        float timeSec = currentMs / 1000f;
        pointerPosition = 0.5f + 0.5f * (float)Math.Sin(2 * Math.PI * pointerSpeed * timeSec);
        if (selectedBoxIndex.HasValue)
        {
            int i = selectedBoxIndex.Value;
            if (!boxLocked[i] && !boxCycleLocked[i])
            {
                bool pointerInHotspot = (pointerPosition >= (hotspotDisplayCenter - hotspotWidth * 0.5f)) &&
                                        (pointerPosition <= (hotspotDisplayCenter + hotspotWidth * 0.5f));
                float boxX = boxCenters[i];
                float hLeft = hotspotDisplayCenter - hotspotWidth * 0.5f;
                float hRight = hotspotDisplayCenter + hotspotWidth * 0.5f;
                bool hotspotAboveBox = (boxX >= hLeft && boxX <= hRight);

                if (isMouseDown)
                {
                    if (pointerInHotspot && hotspotAboveBox)
                        boxFills[i] += fillRateCorrect * deltaTime;
                    else
                        boxFills[i] -= rapidUnfillRate * deltaTime;
                }
                else
                {
                    boxFills[i] -= slowUnfillRate * deltaTime;
                }
                boxFills[i] = Math.Max(0, Math.Min(1, boxFills[i]));
                if (boxFills[i] >= 1)
                    boxLocked[i] = true;
            }
        }
        for (int i = 0; i < numBoxes; i++)
        {
            if (!boxLocked[i] && (selectedBoxIndex == null || selectedBoxIndex.Value != i))
            {
                boxFills[i] = Math.Max(0, boxFills[i] - slowUnfillRate * deltaTime);
            }
        }
        if (boxLocked.All(x => x))
        {
            OnComplete?.Invoke();
            capi.Event.EnqueueMainThreadTask(() => TryClose(), "lockpickingclose");
            ctx.Restore();
            return;
        }
        ctx.Save();
        ctx.NewPath();
        double keyholeCenterY = drawingY + drawingHeight * 0.46;
        double keyholeLeftX = drawingX + 10;
        double keyholeRightX = drawingX + drawingWidth - 10;
        double keyholeArcRadius = 20;
        ctx.MoveTo(keyholeLeftX, keyholeCenterY);
        ctx.Arc(keyholeLeftX, keyholeCenterY, keyholeArcRadius, Math.PI / 2, -Math.PI / 2);
        ctx.LineTo(keyholeRightX, keyholeCenterY - keyholeArcRadius);
        ctx.Arc(keyholeRightX, keyholeCenterY, keyholeArcRadius, -Math.PI / 2, Math.PI / 2);
        ctx.LineTo(keyholeLeftX, keyholeCenterY + keyholeArcRadius);
        ctx.ClosePath();
        ctx.Clip();

        if (lockTextureSurface != null)
        {
            using (var pattern = new Cairo.SurfacePattern(lockTextureSurface))
            {
                double scaleX = drawingWidth / (double)lockTextureSurface.Width;
                double scaleY = drawingHeight / (double)lockTextureSurface.Height;
                pattern.Matrix = new Cairo.Matrix(1.0/scaleX, 0, 0, 1.0/scaleY, -drawingX/scaleX, -drawingY/scaleY);
                ctx.SetSource(pattern);
                ctx.Rectangle(drawingX, drawingY, drawingWidth, drawingHeight);
                ctx.Fill();
            }
        }
        else
        {
            using (var bgPat = new RadialGradient(
                        drawingX + drawingWidth / 2, drawingY + drawingHeight / 2, drawingWidth * 0.1,
                        drawingX + drawingWidth / 2, drawingY + drawingHeight / 2, drawingWidth / 2))
            {
                Color lockColor = GetLockColor();
                Color darkerColor = new Color(lockColor.R * 0.5, lockColor.G * 0.5, lockColor.B * 0.5);
                bgPat.AddColorStop(0, lockColor);
                bgPat.AddColorStop(1, darkerColor);
                ctx.SetSource(bgPat);
                ctx.Rectangle(drawingX, drawingY, drawingWidth, drawingHeight);
                ctx.Fill();
            }
        }
        ctx.Restore();
        DrawMinigameUI(ctx, new ElementBounds() 
        { 
            fixedX = drawingX, 
            fixedY = drawingY, 
            fixedWidth = drawingWidth, 
            fixedHeight = drawingHeight 
        });

        ctx.Restore();
    }

    private void DrawMinigameUI(Context ctx, ElementBounds bounds)
    {
        double areaX = bounds.fixedX;
        double areaY = bounds.fixedY;
        double areaWidth = bounds.fixedWidth;
        double startY = areaY + 60;
        double boxSize = 40;
        double rowGap = 10;
        int totalRows = (int)Math.Ceiling(numBoxes / (double)MaxBoxesPerRow);
        int boxIndex = 0;
        if (totalRows > 0)
        {
            int boxesInFirstRow = Math.Min(numBoxes, MaxBoxesPerRow);
            double spacing = (areaWidth - (boxesInFirstRow * boxSize)) / (boxesInFirstRow + 1);
            double rowY = startY;
            for (int col = 0; col < boxesInFirstRow; col++, boxIndex++)
            {
                double boxX = areaX + spacing + col * (boxSize + spacing);
                DrawTumbler(ctx, boxX, rowY, boxSize, boxIndex);
            }
        }
        double barY = startY + boxSize + 10;
        double meterWidth = areaWidth * 0.8;
        double meterHeight = 20;
        double meterX = areaX + (areaWidth - meterWidth) / 2;
        using (var meterPat = new LinearGradient(meterX, barY, meterX, barY + meterHeight))
        {
            meterPat.AddColorStop(0, new Color(0.6, 0.6, 0.6));
            meterPat.AddColorStop(1, new Color(0.3, 0.3, 0.3));
            ctx.SetSource(meterPat);
            ctx.Rectangle(meterX, barY, meterWidth, meterHeight);
            ctx.Fill();
        }
        double hLeft = meterX + (hotspotDisplayCenter - hotspotWidth / 2.0) * meterWidth;
        double hWidth = hotspotWidth * meterWidth;
        ctx.Rectangle(hLeft, barY, hWidth, meterHeight);
        ctx.SetSourceRGBA(0, 1, 0, 0.5);
        ctx.Fill();
        double pointerX = meterX + pointerPosition * meterWidth;
        ctx.Rectangle(pointerX - 2, barY, 4, meterHeight);
        ctx.SetSourceRGB(1, 1, 1);
        ctx.Fill();
        double originalRow1Y = startY + (boxSize + rowGap);
        double desiredRow1Y = barY + meterHeight + 10;
        double shift = desiredRow1Y - originalRow1Y;
        for (int row = 1; row < totalRows; row++)
        {
            int boxesInThisRow = (row == totalRows - 1) ? numBoxes - row * MaxBoxesPerRow : MaxBoxesPerRow;
            double spacing = (areaWidth - (boxesInThisRow * boxSize)) / (boxesInThisRow + 1);
            double rowY = startY + row * (boxSize + rowGap) + shift;
            for (int col = 0; col < boxesInThisRow; col++, boxIndex++)
            {
                double boxX = areaX + spacing + col * (boxSize + spacing);
                DrawTumbler(ctx, boxX, rowY, boxSize, boxIndex);
            }
        }
    }
    private void DrawRoundedRectangle(Context ctx, double x, double y, double width, double height, double radius)
    {
        ctx.MoveTo(x + radius, y);
        ctx.LineTo(x + width - radius, y);
        ctx.Arc(x + width - radius, y + radius, radius, -Math.PI / 2, 0);
        ctx.LineTo(x + width, y + height - radius);
        ctx.Arc(x + width - radius, y + height - radius, radius, 0, Math.PI / 2);
        ctx.LineTo(x + radius, y + height);
        ctx.Arc(x + radius, y + height - radius, radius, Math.PI / 2, Math.PI);
        ctx.LineTo(x, y + radius);
        ctx.Arc(x + radius, y + radius, radius, Math.PI, 3 * Math.PI / 2);
        ctx.ClosePath();
    }
    private void DrawTumbler(Context ctx, double x, double y, double size, int i)
    {
        ctx.Save();
        if (boxLocked[i])
        {
            if (numBoxes > MaxBoxesPerRow && i >= MaxBoxesPerRow)
            {
                double snapOffset = 5.0;
                ctx.Translate(0, snapOffset);
            }
            else
            {
                double snapOffset = 5.0;
                ctx.Translate(0, -snapOffset);
            }
        }
        else if (selectedBoxIndex.HasValue && selectedBoxIndex.Value == i && isMouseDown)
        {
            double offset = Math.Sin(capi.ElapsedMilliseconds / 100.0) * 2;
            ctx.Translate(0, offset);
        }
        double radius = 5;
        Color lockColor = GetLockColor();
        Color lighter = new Color(
            Math.Min(lockColor.R + 0.2, 1.0),
            Math.Min(lockColor.G + 0.2, 1.0),
            Math.Min(lockColor.B + 0.2, 1.0)
        );
        Color darker = new Color(
            lockColor.R * 0.7,
            lockColor.G * 0.7,
            lockColor.B * 0.7
        );
        if (lockTextureSurface != null && tumblerCropOffsets != null && tumblerCropOffsets.Length > i)
        {
            double cropSize = 16.0;
            double scaleFactor = size / cropSize;
            using (var pattern = new Cairo.SurfacePattern(lockTextureSurface))
            {
                pattern.Matrix = new Cairo.Matrix(
                    1.0 / scaleFactor, 0,
                    0, 1.0 / scaleFactor,
                    -x / scaleFactor + tumblerCropOffsets[i].Item1,
                    -y / scaleFactor + tumblerCropOffsets[i].Item2
                );
                ctx.SetSource(pattern);
                DrawRoundedRectangle(ctx, x, y, size, size, radius);
                ctx.FillPreserve();
            }
        }
        else
        {
            using (var pat = new LinearGradient(x, y, x, y + size))
            {
                pat.AddColorStop(0, lighter);
                pat.AddColorStop(1, darker);
                ctx.SetSource(pat);
                DrawRoundedRectangle(ctx, x, y, size, size, radius);
                ctx.FillPreserve();
            }
        }
        ctx.SetSourceRGB(0.1, 0.1, 0.1);
        ctx.Stroke();
        if (boxLocked[i])
        {
            ctx.SetSourceRGBA(0, 1, 0, 0.5);
            DrawRoundedRectangle(ctx, x, y, size, size, radius);
            ctx.Fill();
        }
        else if (boxFills[i] > 0)
        {
            double fillHeight = size * boxFills[i];
            double fillY = y + (size - fillHeight);
            using (var pat = new LinearGradient(x, fillY, x, y + size))
            {
                pat.AddColorStop(0, new Color(0.5, 0.5, 1));
                pat.AddColorStop(1, new Color(0, 0, 0.8));
                ctx.SetSource(pat);
                DrawRoundedRectangle(ctx, x, fillY, size, fillHeight, radius);
                ctx.Fill();
            }
        }
        if (boxCycleLocked[i])
        {
            float fadeAlpha = cycleLockTimers[i] / cycleActiveDuration;
            DrawPadlockIcon(ctx, x, y, size, fadeAlpha);
        }
        ctx.Restore();
    }
    private void DrawPadlockIcon(Context ctx, double x, double y, double size, float alpha)
    {
        double offset = -size * 0.05;
        double bodyWidth = size * 0.6;
        double bodyHeight = size * 0.4;
        double bodyX = x + (size - bodyWidth) / 2;
        double bodyY = y + size * 0.55 + offset;
        Color metalColor = GetLockColor();
        ctx.LineWidth = 2;
        ctx.SetSourceRGBA(metalColor.R, metalColor.G, metalColor.B, alpha);
        ctx.NewPath();
        double shackleRadius = bodyWidth / 2;
        ctx.Arc(x + size / 2, y + size * 0.55 + offset, shackleRadius, Math.PI, 2 * Math.PI);
        ctx.Stroke();
        ctx.SetSourceRGB(0, 0, 0);
        ctx.LineWidth = 1;
        ctx.NewPath();
        ctx.Arc(x + size / 2, y + size * 0.55 + offset, shackleRadius, Math.PI, 2 * Math.PI);
        ctx.Stroke();
        ctx.SetSourceRGBA(metalColor.R, metalColor.G, metalColor.B, alpha);
        ctx.Rectangle(bodyX, bodyY, bodyWidth, bodyHeight);
        ctx.Fill();
        ctx.SetSourceRGB(0, 0, 0);
        ctx.LineWidth = 1;
        ctx.Rectangle(bodyX, bodyY, bodyWidth, bodyHeight);
        ctx.Stroke();
        double keyholeCircleRadius = bodyWidth * 0.1;
        double keyholeCircleCenterX = x + size / 2;
        double keyholeCircleCenterY = bodyY + bodyHeight * 0.4;
        ctx.SetSourceRGB(0, 0, 0);
        ctx.NewPath();
        ctx.Arc(keyholeCircleCenterX, keyholeCircleCenterY, keyholeCircleRadius, 0, 2 * Math.PI);
        ctx.Fill();

        double slotWidth = keyholeCircleRadius * 0.6;
        double slotHeight = bodyHeight * 0.25;
        double slotX = keyholeCircleCenterX - slotWidth / 2;
        double slotY = keyholeCircleCenterY + keyholeCircleRadius;
        ctx.Rectangle(slotX, slotY, slotWidth, slotHeight);
        ctx.Fill();
    }
}
