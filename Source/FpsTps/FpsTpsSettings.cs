using Verse;

namespace Anno.FpsTps;

public sealed class FpsTpsSettings : ModSettings
{
    public bool Enabled = true;
    public bool ShowFps = true;
    public bool ShowTps = true;
    public bool ShowTarget = true;
    public bool ShowPercent;
    public bool ShowFrameTime;
    public bool ShowTickTime;
    public bool ShowWorld = true;
    public bool HidePaused;
    public bool SuppressVanilla = true;
    public bool Horizontal;
    public bool TpsFirst;
    public bool TinyFont;
    public bool AutoWidth = true;
    public bool Colorize;
    public float FpsInterval = 0.5f;
    public float TpsInterval = 1f;
    public int Decimals;
    public float Width = 240f;
    public float RowHeight = 26f;
    public float RightInset;
    public float Gap;
    public float BackgroundAlpha;
    public float GoodFps = 60f;
    public float WarningFps = 30f;
    public float GoodTpsRatio = 0.95f;
    public float WarningTpsRatio = 0.7f;
    public int Palette;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Enabled, "enabled", true);
        Scribe_Values.Look(ref ShowFps, "showFps", true);
        Scribe_Values.Look(ref ShowTps, "showTps", true);
        Scribe_Values.Look(ref ShowTarget, "showTarget", true);
        Scribe_Values.Look(ref ShowPercent, "showPercent");
        Scribe_Values.Look(ref ShowFrameTime, "showFrameTime");
        Scribe_Values.Look(ref ShowTickTime, "showTickTime");
        Scribe_Values.Look(ref ShowWorld, "showWorld", true);
        Scribe_Values.Look(ref HidePaused, "hidePaused");
        Scribe_Values.Look(ref SuppressVanilla, "suppressVanilla", true);
        Scribe_Values.Look(ref Horizontal, "horizontal");
        Scribe_Values.Look(ref TpsFirst, "tpsFirst");
        Scribe_Values.Look(ref TinyFont, "tinyFont");
        Scribe_Values.Look(ref AutoWidth, "autoWidth", true);
        Scribe_Values.Look(ref Colorize, "colorize");
        Scribe_Values.Look(ref FpsInterval, "fpsInterval", 0.5f);
        Scribe_Values.Look(ref TpsInterval, "tpsInterval", 1f);
        Scribe_Values.Look(ref Decimals, "decimals");
        Scribe_Values.Look(ref Width, "width", 240f);
        Scribe_Values.Look(ref RowHeight, "rowHeight", 26f);
        Scribe_Values.Look(ref RightInset, "rightInset");
        Scribe_Values.Look(ref Gap, "gap");
        Scribe_Values.Look(ref BackgroundAlpha, "backgroundAlpha");
        Scribe_Values.Look(ref GoodFps, "goodFps", 60f);
        Scribe_Values.Look(ref WarningFps, "warningFps", 30f);
        Scribe_Values.Look(ref GoodTpsRatio, "goodTpsRatio", 0.95f);
        Scribe_Values.Look(ref WarningTpsRatio, "warningTpsRatio", 0.7f);
        Scribe_Values.Look(ref Palette, "palette");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            Sanitize();
    }

    public void Sanitize()
    {
        FpsInterval = Clamp(FpsInterval, 0.1f, 5f, 0.5f);
        TpsInterval = Clamp(TpsInterval, 0.1f, 5f, 1f);
        Decimals = Math.Clamp(Decimals, 0, 2);
        Width = Clamp(Width, 140f, 600f, 240f);
        RowHeight = Clamp(RowHeight, 22f, 36f, 26f);
        RightInset = Clamp(RightInset, 0f, 200f, 0f);
        Gap = Clamp(Gap, 0f, 24f, 0f);
        BackgroundAlpha = Clamp(BackgroundAlpha, 0f, 0.9f, 0f);
        GoodFps = Clamp(GoodFps, 10f, 240f, 60f);
        WarningFps = Clamp(WarningFps, 1f, GoodFps, 30f);
        GoodTpsRatio = Clamp(GoodTpsRatio, 0.1f, 1f, 0.95f);
        WarningTpsRatio = Clamp(WarningTpsRatio, 0.05f, GoodTpsRatio, 0.7f);
        Palette = Math.Clamp(Palette, 0, 2);
    }

    private static float Clamp(float value, float min, float max, float fallback) =>
        float.IsFinite(value) ? Math.Clamp(value, min, max) : Math.Clamp(fallback, min, max);
}
