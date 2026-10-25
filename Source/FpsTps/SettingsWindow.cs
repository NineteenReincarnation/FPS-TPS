using UnityEngine;
using Verse;

namespace Anno.FpsTps;

internal sealed class SettingsWindow
{
    private int tab;
    private Vector2 scroll;
    private float contentHeight = 1200f;
    private readonly Listing_Standard list = new();
    private static readonly string[] Tabs = ["Display", "Sampling", "Appearance", "Help"];
    private static string T(string key) => ("AT_" + key).Translate();

    internal void Draw(Rect area)
    {
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Color oldColor = GUI.color;
        try
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            float tabWidth = (area.width - 18f) / 4f;
            for (int i = 0; i < Tabs.Length; i++)
            {
                Rect button = new(area.x + i * (tabWidth + 6f), area.y, tabWidth, 32f);
                if (i == tab)
                    Widgets.DrawBoxSolid(button, new Color(0.22f, 0.28f, 0.34f));
                if (Widgets.ButtonText(button, T(Tabs[i])))
                {
                    tab = i;
                    scroll = Vector2.zero;
                    contentHeight = 1200f;
                }
            }
            Rect body = new(area.x, area.y + 42f, area.width, area.height - 92f);
            Rect view = new(0, 0, body.width - 20f, contentHeight);
            Widgets.BeginScrollView(body, ref scroll, view);
            list.Begin(new Rect(0, 0, view.width, 10000f));
            switch (tab)
            {
                case 0: Display(); break;
                case 1: Sampling(); break;
                case 2: Appearance(); break;
                case 3: Help(); break;
            }
            contentHeight = list.CurHeight + 12f;
            list.End();
            Widgets.EndScrollView();

            float buttonY = area.yMax - 36f;
            if (Widgets.ButtonText(new Rect(area.x, buttonY, area.width * 0.32f, 32f), T("Defaults")))
            {
                // One-off UI operation, never part of sampling or drawing the counters.
                FpsTpsSettings defaults = new();
                foreach (var field in typeof(FpsTpsSettings).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    field.SetValue(FpsTpsMod.Settings, field.GetValue(defaults));
                CounterOverlay.Reset();
            }
            if (Widgets.ButtonText(new Rect(area.xMax - area.width * 0.64f, buttonY, area.width * 0.31f, 32f), T("Minimal")))
            {
                FpsTpsSettings s = FpsTpsMod.Settings;
                s.ShowFrameTime = s.ShowTickTime = s.ShowPercent = s.Colorize = false;
                s.FpsInterval = 1f;
                s.TpsInterval = 2f;
                s.BackgroundAlpha = 0;
                CounterOverlay.Reset();
            }
            if (Widgets.ButtonText(new Rect(area.xMax - area.width * 0.31f, buttonY, area.width * 0.31f, 32f), T("Detailed")))
            {
                FpsTpsSettings s = FpsTpsMod.Settings;
                s.ShowFps = s.ShowTps = s.ShowTarget = s.ShowPercent = s.ShowFrameTime = s.ShowTickTime = true;
                s.Colorize = s.AutoWidth = true;
                s.FpsInterval = 0.5f;
                s.TpsInterval = 1f;
                s.Decimals = 1;
                CounterOverlay.Reset();
            }
        }
        finally
        {
            GUI.color = oldColor;
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
        }
    }

    private void Check(string key, ref bool value)
    {
        bool before = value;
        list.CheckboxLabeled(T(key), ref value, T(key + "Tip"));
        if (before != value)
            CounterOverlay.Reset();
    }

    private void Slider(string key, ref float value, float min, float max, string format, float step)
    {
        float before = value;
        list.Label(T(key) + ": " + value.ToString(format, System.Globalization.CultureInfo.InvariantCulture), tooltip: T(key + "Tip"));
        value = (float)Math.Round(list.Slider(value, min, max) / step) * step;
        value = Math.Clamp(value, min, max);
        list.Gap(4f);
        if (before != value)
            CounterOverlay.Reset();
    }

    private void Heading(string key)
    {
        list.Gap(8f);
        list.GapLine();
        list.Label(T(key));
        list.Gap(6f);
    }

    private void Display()
    {
        FpsTpsSettings s = FpsTpsMod.Settings;
        Check("Enabled", ref s.Enabled);
        Heading("Metrics");
        Check("ShowFps", ref s.ShowFps);
        Check("ShowTps", ref s.ShowTps);
        Check("ShowTarget", ref s.ShowTarget);
        Check("ShowPercent", ref s.ShowPercent);
        Check("ShowFrameTime", ref s.ShowFrameTime);
        Check("ShowTickTime", ref s.ShowTickTime);
        Heading("Visibility");
        Check("ShowWorld", ref s.ShowWorld);
        Check("HidePaused", ref s.HidePaused);
        Check("SuppressVanilla", ref s.SuppressVanilla);
    }

    private void Sampling()
    {
        FpsTpsSettings s = FpsTpsMod.Settings;
        list.Label(T("SamplingInfo"));
        Slider("FpsInterval", ref s.FpsInterval, 0.1f, 5f, "F1", 0.1f);
        Slider("TpsInterval", ref s.TpsInterval, 0.1f, 5f, "F1", 0.1f);
        float decimals = s.Decimals;
        Slider("Decimals", ref decimals, 0, 2, "F0", 1f);
        s.Decimals = (int)decimals;
        Heading("MethodHeading");
        list.Label(T("MethodInfo"));
        Heading("PauseHeading");
        list.Label(T("PauseInfo"));
    }

    private void Appearance()
    {
        FpsTpsSettings s = FpsTpsMod.Settings;
        Check("Horizontal", ref s.Horizontal);
        Check("TpsFirst", ref s.TpsFirst);
        Check("TinyFont", ref s.TinyFont);
        Check("AutoWidth", ref s.AutoWidth);
        Slider("Width", ref s.Width, 140, 600, "F0", 10);
        Slider("RowHeight", ref s.RowHeight, 22, 36, "F0", 1);
        Slider("RightInset", ref s.RightInset, 0, 200, "F0", 1);
        Slider("Gap", ref s.Gap, 0, 24, "F0", 1);
        Slider("BackgroundAlpha", ref s.BackgroundAlpha, 0, 0.9f, "P0", 0.05f);
        Heading("ColorHeading");
        Check("Colorize", ref s.Colorize);
        if (list.ButtonTextLabeled(T("Palette"), T("Palette" + s.Palette)))
        {
            s.Palette = (s.Palette + 1) % 3;
            CounterOverlay.Reset();
        }
        Slider("GoodFps", ref s.GoodFps, 10, 240, "F0", 1);
        Slider("WarningFps", ref s.WarningFps, 1, s.GoodFps, "F0", 1);
        Slider("GoodTpsRatio", ref s.GoodTpsRatio, 0.1f, 1f, "P0", 0.01f);
        Slider("WarningTpsRatio", ref s.WarningTpsRatio, 0.05f, s.GoodTpsRatio, "P0", 0.01f);
        s.Sanitize();
    }

    private void Help()
    {
        list.Label("FPS & TPS 1.2.2 | anno | anno.fpsandtps");
        Heading("HelpMeaning");
        list.Label(T("HelpMeaningText"));
        Heading("HelpPerformance");
        list.Label(T("HelpPerformanceText"));
        Heading("HelpCompatibility");
        list.Label(T("HelpCompatibilityText"));
        Heading("HelpPresets");
        list.Label(T("HelpPresetsText"));
    }
}
