using UnityEngine;
using Verse;

namespace Anno.FpsTps;

/// <summary>Official game extension point. No saved fields and no tick/update override.</summary>
public sealed class FpsTpsGameComponent : GameComponent
{
    private readonly CounterWindow window = new();
    public FpsTpsGameComponent(Game game) { }

    public override void FinalizeInit() => CounterOverlay.Reset();

    public override void GameComponentOnGUI()
    {
        // Restore first if a previous native UI pass ended early.
        NativeCounterVisibility.Restore();
        if (Current.ProgramState != ProgramState.Playing)
            return;
        WindowStack? stack = Find.WindowStack;
        if (stack is null)
            return;
        if (!stack.IsOpen(window))
            stack.Add(window);
        if (Event.current.type == EventType.repaint && CounterOverlay.IsVisible)
            NativeCounterVisibility.Suppress(FpsTpsMod.Settings);
    }
}

/// <summary>Late native GUI layer: reads the game's completed layout, never captures input.</summary>
internal sealed class CounterWindow : Window
{
    internal CounterWindow()
    {
        layer = WindowLayer.GameUI;
        closeOnAccept = closeOnCancel = closeOnClickedOutside = false;
        forcePause = preventCameraMotion = preventSave = focusWhenOpened = false;
        doWindowBackground = drawShadow = absorbInputAroundWindow = false;
        soundAppear = soundClose = null;
        drawInScreenshotMode = false;
    }

    protected override void SetInitialSizeAndPosition() => windowRect = new Rect(-10000, -10000, 0, 0);
    public override void DoWindowContents(Rect inRect) { }

    public override void WindowOnGUI()
    {
        try
        {
            if (Event.current.type != EventType.repaint || !CounterOverlay.IsVisible)
                return;
            // Public LastTopY is updated by the colony/world controls earlier in this GUI pass.
            // Stay above the whole native stack, including letters, without private-field access.
            float bottom = Find.LetterStack.LastTopY;
            float height = CounterOverlay.Height;
            bottom = Math.Max(height + 4, Math.Min(bottom, UI.screenHeight - 35));
            CounterOverlay.Draw(UI.screenWidth - 200, 200, ref bottom);
        }
        finally
        {
            NativeCounterVisibility.Restore();
        }
    }

    public override void PreClose() => NativeCounterVisibility.Restore();
}

/// <summary>Hide duplicates only during native drawing, restoring both user flags afterwards.</summary>
internal static class NativeCounterVisibility
{
    private static bool active;
    private static bool fps;
    private static bool tps;

    internal static void Suppress(FpsTpsSettings settings)
    {
        Restore();
        if (!settings.SuppressVanilla)
            return;
        fps = DebugViewSettings.showFpsCounter;
        tps = DebugViewSettings.showTpsCounter;
        active = true;
        if (settings.ShowFps)
            DebugViewSettings.showFpsCounter = false;
        if (settings.ShowTps)
            DebugViewSettings.showTpsCounter = false;
    }

    internal static void Restore()
    {
        if (!active)
            return;
        DebugViewSettings.showFpsCounter = fps;
        DebugViewSettings.showTpsCounter = tps;
        active = false;
    }
}
