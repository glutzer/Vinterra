using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

/// <summary>
/// Element above coordinates that shows biome.
/// </summary>
public class HudElementBiome : HudElement
{
    public override string ToggleKeyCombinationCode => "coordinateshud";

    public HudElementBiome(ICoreClientAPI capi) : base(capi)
    {
    }

    public override void OnOwnPlayerDataReceived()
    {
        ElementBounds elementBounds = ElementBounds.Fixed(EnumDialogArea.None, 0.0, 0.0, 190.0, 28.0);
        ElementBounds bounds = elementBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
        ElementBounds boundsPadding = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightTop).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);
        SingleComposer = capi.Gui.CreateCompo("biomehud", boundsPadding).AddGameOverlay(bounds).AddDynamicText("", CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center), elementBounds, "text").Compose();
        if (ClientSettings.ShowCoordinateHud)
        {
            TryOpen();
        }
    }

    public override void OnBlockTexturesLoaded()
    {
        base.OnBlockTexturesLoaded();
        if (!capi.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
        {
            (capi.World as ClientMain).EnqueueMainThreadTask(delegate
            {
                (capi.World as ClientMain).UnregisterDialog(this);
                capi.Input.SetHotKeyHandler("coordinateshud", null);
                Dispose();
            }, "unreg");
            return;
        }

        capi.Event.RegisterGameTickListener(Every1000ms, 1000);
        ClientSettings.Inst.AddWatcher("showCoordinateHud", delegate (bool on)
        {
            if (on)
            {
                TryOpen();
            }
            else
            {
                TryClose();
            }
        });
    }

    private void Every1000ms(float dt)
    {
        if (!IsOpened())
        {
            return;
        }

        string biomeText = VinterraMod.lastBiome;
        SingleComposer.GetDynamicText("text").SetNewText(biomeText);
        List<ElementBounds> dialogBoundsInArea = capi.Gui.GetDialogBoundsInArea(EnumDialogArea.RightTop);
        SingleComposer.Bounds.absOffsetY = GuiStyle.DialogToScreenPadding;

        for (int i = 0; i < dialogBoundsInArea.Count; i++)
        {
            if (dialogBoundsInArea[i] != SingleComposer.Bounds && dialogBoundsInArea[i].InnerWidth != 175) //175 is the coordinate dialogue and it plays leapfrog if moved under it
            {
                ElementBounds elementBounds = dialogBoundsInArea[i];
                SingleComposer.Bounds.absOffsetY = GuiStyle.DialogToScreenPadding + elementBounds.absY + elementBounds.OuterHeight;
                break;
            }
        }
    }

    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        ClientSettings.ShowCoordinateHud = true;
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        ClientSettings.ShowCoordinateHud = false;
    }
}