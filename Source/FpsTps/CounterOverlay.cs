using System.Diagnostics;
using UnityEngine;
using Verse;

namespace Anno.FpsTps;

internal static class CounterOverlay
{
    private const float RowHeight = 26f;
    private const double FpsInterval = 0.5;
    private const double TpsInterval = 1;
    private static readonly MetricText FpsText = new();
    private static readonly MetricText TpsText = new();
    private static TpsReadout readout;
    private static System.WeakReference<TickManager>? manager;
    private static double nextFpsTime;
    private static double nextTargetTime;
    private static int previousSpeed;
    private static bool paused;
    private static float target;
    private static int lastFrame = -1;
    private static int measuredFpsRevision = -1;
    private static int measuredTpsRevision = -1;
    private static float fpsWidth;
    private static float tpsWidth;

    internal static void Reset()
    {
        readout = default;
        manager = null;
        nextFpsTime = nextTargetTime = 0;
        lastFrame = -1;
        measuredFpsRevision = measuredTpsRevision = -1;
    }

    private static bool Visible(FpsTpsSettings settings) => (settings.ShowFps || settings.ShowTps)
        && Current.ProgramState == ProgramState.Playing
        && Find.UIRoot is { } root && !root.screenshotMode.FiltersCurrentEvent;

    internal static bool ReplacesVanilla(bool fpsCounter)
    {
        FpsTpsSettings settings = FpsTpsMod.Settings;
        return (fpsCounter ? settings.ShowFps : settings.ShowTps) && Visible(settings);
    }

    internal static void Draw(float leftX, float nativeWidth, ref float baseY)
    {
        FpsTpsSettings settings = FpsTpsMod.Settings;
        if (!Visible(settings))
        {
            if (manager is not null)
                Reset();
            return;
        }
        int rows = (settings.ShowFps ? 1 : 0) + (settings.ShowTps ? 1 : 0);
        float top = baseY - rows * RowHeight;
        baseY = top;
        if (Event.current.type != EventType.repaint)
            return;
        if (lastFrame != RealTime.frameCount)
        {
            lastFrame = RealTime.frameCount;
            Refresh(settings);
        }

        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Color oldColor = GUI.color;
        bool oldWrap = Text.WordWrap;
        try
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleRight;
            Text.WordWrap = false;
            GUI.color = Color.white;
            if (settings.ShowFps && measuredFpsRevision != FpsText.Revision)
            {
                fpsWidth = Text.CalcSize(FpsText.Text).x;
                measuredFpsRevision = FpsText.Revision;
            }
            if (settings.ShowTps && measuredTpsRevision != TpsText.Revision)
            {
                tpsWidth = Text.CalcSize(TpsText.Text).x;
                measuredTpsRevision = TpsText.Revision;
            }
            float textWidth = Math.Max(settings.ShowFps ? fpsWidth : 0, settings.ShowTps ? tpsWidth : 0);
            float right = leftX + nativeWidth - 7f;
            float width = Math.Min(Math.Max(nativeWidth - 7f, textWidth + 8f), Math.Max(1, right - 4f));
            Rect row = new(right - width, top, width, RowHeight);
            if (settings.ShowFps)
            {
                Widgets.Label(row, FpsText.Text);
                row.y += RowHeight;
            }
            if (settings.ShowTps)
                Widgets.Label(row, TpsText.Text);
        }
        finally
        {
            GUI.color = oldColor;
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            Text.WordWrap = oldWrap;
        }
    }

    private static void Refresh(FpsTpsSettings settings)
    {
        TickManager current = Find.TickManager;
        if (manager is null || !manager.TryGetTarget(out TickManager? previous) || !ReferenceEquals(previous, current))
        {
            manager = new System.WeakReference<TickManager>(current);
            readout = default;
            nextFpsTime = nextTargetTime = 0;
        }
        double now = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        if (settings.ShowFps && now >= nextFpsTime)
        {
            double frameTime = Root.AverageFrameTime;
            FpsText.Update(true, frameTime > 0 && double.IsFinite(frameTime) ? 1000 / frameTime : 0);
            nextFpsTime = now + FpsInterval;
        }
        if (settings.ShowTps)
        {
            bool wasPaused = paused;
            paused = current.Paused;
            int speed = (int)current.CurTimeSpeed;
            if (now >= nextTargetTime || speed != previousSpeed || paused != wasPaused)
            {
                target = paused ? 0 : Math.Max(0, current.TickRateMultiplier * 60f);
                nextTargetTime = now + TpsInterval;
                previousSpeed = speed;
            }
            if (readout.Advance(now, current.TicksGame, speed, paused, target, TpsInterval))
                TpsText.Update(false, readout.Value, readout.Target);
        }
    }
}
