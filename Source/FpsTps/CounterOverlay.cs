using System.Diagnostics;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Anno.FpsTps;

internal static class CounterOverlay
{
    private static readonly MetricText FpsText = new();
    private static readonly MetricText TpsText = new();
    private static readonly Color[] Healthy = [new(0.55f, 1f, 0.55f), new(0.35f, 0.8f, 1f), Color.white];
    private static readonly Color[] Warning = [new(1f, 0.85f, 0.35f), new(1f, 0.8f, 0.3f), new(1f, 0.85f, 0.35f)];
    private static readonly Color[] Critical = [new(1f, 0.4f, 0.4f), new(1f, 0.5f, 0.2f), new(1f, 0.45f, 0.55f)];
    private static RateSampler sampler;
    // Do not keep a closed colony's tick lists (and their Things) alive from a static UI cache.
    private static System.WeakReference<TickManager>? manager;
    private static double nextFpsTime;
    private static double fps;
    private static double frameTime;
    private static double tickTime;
    private static float target;
    private static double nextTargetTime;
    private static TimeSpeed previousSpeed;
    private static bool paused;
    private static int lastFrame = -1;
    private static int measuredFpsRevision = -1;
    private static int measuredTpsRevision = -1;
    private static GameFont measuredFont;
    private static float fpsWidth;
    private static float tpsWidth;

    internal static void Reset()
    {
        sampler = default;
        manager = null;
        nextFpsTime = 0;
        nextTargetTime = 0;
        lastFrame = -1;
        measuredFpsRevision = measuredTpsRevision = -1;
    }

    private static bool Visible(FpsTpsSettings s) => s.Enabled && (s.ShowFps || s.ShowTps)
        && Current.ProgramState == ProgramState.Playing
        && (s.ShowWorld || !WorldRendererUtility.WorldSelected)
        && (!s.HidePaused || !Find.TickManager.Paused);

    internal static bool ReplacesVanilla(bool fpsCounter)
    {
        FpsTpsSettings s = FpsTpsMod.Settings;
        return s.SuppressVanilla && (fpsCounter ? s.ShowFps : s.ShowTps) && Visible(s);
    }

    internal static void Draw(float leftX, float nativeWidth, ref float baseY)
    {
        FpsTpsSettings s = FpsTpsMod.Settings;
        if (!Visible(s))
        {
            // A short pause with HidePaused enabled must not leak into the next TPS window.
            if (manager is not null)
                Reset();
            return;
        }

        int rows = s.Horizontal || !(s.ShowFps && s.ShowTps) ? 1 : 2;
        float height = rows * s.RowHeight;
        float bottom = baseY - s.Gap;
        baseY -= height + s.Gap;
        // Reserve exactly the same space on input events; draw and sample only on repaint.
        if (Event.current.type != EventType.repaint)
            return;

        if (lastFrame != RealTime.frameCount)
        {
            lastFrame = RealTime.frameCount;
            Refresh(s);
        }
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Color oldColor = GUI.color;
        bool oldWrap = Text.WordWrap;
        try
        {
            Text.Font = s.TinyFont ? GameFont.Tiny : GameFont.Small;
            Text.Anchor = TextAnchor.MiddleRight;
            Text.WordWrap = false;
            Measure();
            float required = s.Horizontal && s.ShowFps && s.ShowTps
                ? fpsWidth + tpsWidth + 20f : Math.Max(s.ShowFps ? fpsWidth : 0, s.ShowTps ? tpsWidth : 0);
            float right = leftX + nativeWidth - 7f - s.RightInset;
            float panelWidth = s.AutoWidth ? Math.Max(s.Width, required + 8f) : s.Width;
            panelWidth = Math.Min(panelWidth, Math.Max(1f, right - 4f));
            Rect bounds = new(right - panelWidth, bottom - height, panelWidth, height);
            if (s.BackgroundAlpha > 0)
                Widgets.DrawBoxSolid(bounds, new Color(0f, 0f, 0f, s.BackgroundAlpha));

            if (s.ShowFps && s.ShowTps && s.Horizontal)
            {
                // Scale the two columns when automatic width is disabled.
                float firstWidth = s.TpsFirst ? tpsWidth : fpsWidth;
                float secondWidth = s.TpsFirst ? fpsWidth : tpsWidth;
                float secondShare = (secondWidth + 4f) / (firstWidth + secondWidth + 8f);
                float split = bounds.width * secondShare;
                Rect first = new(bounds.x, bounds.y, bounds.width - split - 12f, bounds.height);
                Rect second = new(bounds.xMax - split, bounds.y, split, bounds.height);
                DrawMetric(first, !s.TpsFirst, s);
                DrawMetric(second, s.TpsFirst, s);
            }
            else
            {
                Rect row = new(bounds.x, bounds.y, bounds.width, s.RowHeight);
                if (s.ShowFps && s.ShowTps)
                {
                    DrawMetric(row, !s.TpsFirst, s);
                    row.y += s.RowHeight;
                    DrawMetric(row, s.TpsFirst, s);
                }
                else
                    DrawMetric(row, s.ShowFps, s);
            }
        }
        finally
        {
            GUI.color = oldColor;
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            Text.WordWrap = oldWrap;
        }
    }

    private static void Refresh(FpsTpsSettings s)
    {
        TickManager current = Find.TickManager;
        if (manager is null || !manager.TryGetTarget(out TickManager? previous) || !ReferenceEquals(previous, current))
        {
            manager = new System.WeakReference<TickManager>(current);
            sampler = default;
            nextFpsTime = 0;
            nextTargetTime = 0;
        }
        double now = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        if (s.ShowFps && now >= nextFpsTime)
        {
            frameTime = Root.AverageFrameTime;
            fps = frameTime > 0 && double.IsFinite(frameTime) ? 1000d / frameTime : 0;
            FpsText.Update(true, fps, 0, frameTime, s.Decimals,
                s.ShowFrameTime ? MetricParts.Cost : MetricParts.None);
            nextFpsTime = now + s.FpsInterval;
        }
        if (s.ShowTps)
        {
            bool wasPaused = paused;
            paused = current.Paused;
            // This getter can inspect awake colonists at 3x speed. Read it only on state changes
            // or at the TPS interval, never on every repaint. Respect mods patching the multiplier.
            TimeSpeed speed = current.CurTimeSpeed;
            if (now >= nextTargetTime || speed != previousSpeed || wasPaused != paused)
            {
                target = paused ? 0 : Math.Max(0, current.TickRateMultiplier * 60f);
                nextTargetTime = now + s.TpsInterval;
                previousSpeed = speed;
            }
            if (sampler.Advance(now, current.TicksGame, paused, target, s.TpsInterval) || wasPaused != paused)
            {
                tickTime = paused ? 0 : current.MeanTickTime;
                MetricParts parts = (s.ShowTarget ? MetricParts.Target : 0)
                    | (s.ShowPercent ? MetricParts.Percent : 0) | (s.ShowTickTime ? MetricParts.Cost : 0);
                TpsText.Update(false, sampler.Value, target, tickTime, s.Decimals, parts, paused);
            }
        }
    }

    private static void Measure()
    {
        if (measuredFont != Text.Font)
        {
            measuredFont = Text.Font;
            measuredFpsRevision = measuredTpsRevision = -1;
        }
        if (measuredFpsRevision != FpsText.Revision)
        {
            fpsWidth = Text.CalcSize(FpsText.Text).x;
            measuredFpsRevision = FpsText.Revision;
        }
        if (measuredTpsRevision != TpsText.Revision)
        {
            tpsWidth = Text.CalcSize(TpsText.Text).x;
            measuredTpsRevision = TpsText.Revision;
        }
    }

    private static void DrawMetric(Rect rect, bool isFps, FpsTpsSettings s)
    {
        Color color = Color.white;
        if (s.Colorize && (isFps || (!paused && sampler.Ready)))
        {
            double value = isFps ? fps : target > 0 ? sampler.Value / target : 0;
            double good = isFps ? s.GoodFps : s.GoodTpsRatio;
            double warn = isFps ? s.WarningFps : s.WarningTpsRatio;
            color = value >= good ? Healthy[s.Palette] : value >= warn ? Warning[s.Palette] : Critical[s.Palette];
        }
        GUI.color = color;
        Widgets.Label(rect, isFps ? FpsText.Text : TpsText.Text);
    }
}
