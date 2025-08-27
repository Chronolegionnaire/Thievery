using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Thievery.LockAndKey
{
    public class KeyNamingDialog : GuiDialogGeneric
    {
        private readonly ItemSlot slot;
        private bool didSave;
        public event Action<string> OnNameSet;

        public KeyNamingDialog(ItemSlot slot, ICoreClientAPI capi)
            : base(Lang.Get("thievery:keyname-title"), capi)
        {
            this.slot = slot;
            ElementBounds line = ElementBounds.Fixed(0, 0, 300, 20);
            ElementBounds input = ElementBounds.Fixed(0, 20, 300, 30);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.CenterFixed)
                .WithFixedAlignmentOffset(0, 0);

            float inputLineY = 40f;
            float buttonY = 70f;

            SingleComposer = capi.Gui
                .CreateCompo("keynamingdialog", dialogBounds)
                .AddShadedDialogBG(bgBounds, true, 5.0, 0.75f)
                .AddDialogTitleBar(Lang.Get("thievery:keyname-title"), OnTitleBarClose)
                .BeginChildElements(bgBounds)
                .AddStaticText(Lang.Get("thievery:keyname-label"), CairoFont.WhiteDetailText(), line = line.BelowCopy(0, 0, 0, 0))
                .AddTextInput(input = input.BelowCopy(0, inputLineY, 0, 0).WithFixedWidth(250), OnTextChanged, CairoFont.WhiteSmallText(), "keyNameInput")
                .AddSmallButton(Lang.Get("thievery:common-cancel"), OnCancelButtonClicked, input = input.BelowCopy(0, buttonY, 0, 0).WithFixedSize(100, 30).WithAlignment(EnumDialogArea.LeftFixed))
                .AddSmallButton(Lang.Get("thievery:common-save"), OnSaveButtonClicked, input.FlatCopy().WithFixedSize(100, 30).WithAlignment(EnumDialogArea.RightFixed))
                .EndChildElements()
                .Compose();
        }

        public override string ToggleKeyCombinationCode => null;

        private void OnTitleBarClose() => TryClose();

        private void OnTextChanged(string text) { }

        private bool OnCancelButtonClicked()
        {
            TryClose();
            return true;
        }

        private bool OnSaveButtonClicked()
        {
            string keyName = SingleComposer.GetTextInput("keyNameInput").GetText();

            if (!string.IsNullOrEmpty(keyName))
            {
                slot.Itemstack.Attributes.SetString("keyName", keyName);
                slot.MarkDirty();
                didSave = true;
                capi.Network.GetChannel("thievery").SendPacket(new KeyNameUpdatePacket
                {
                    SlotId = capi.World.Player.InventoryManager.ActiveHotbarSlotNumber,
                    KeyName = keyName
                });
                OnNameSet?.Invoke(keyName);
            }

            TryClose();
            return true;
        }

        public override void OnGuiClosed()
        {
            if (!didSave)
            {
            }
            base.OnGuiClosed();
        }
    }
}
