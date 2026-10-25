using UnityEngine;
using Verse;

namespace Anno.FpsTps;

public sealed class FpsTpsMod : Mod
{
    internal static FpsTpsSettings Settings = new();
    private readonly SettingsWindow settingsWindow = new();

    public FpsTpsMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<FpsTpsSettings>();
        Settings.Sanitize();
        Log.Message("[FPS & TPS] anno.fpsandtps 1.1.0 loaded. No prerequisite mods or runtime patches.");
    }

    public override string SettingsCategory() => "FPS & TPS";
    public override void DoSettingsWindowContents(Rect inRect) => settingsWindow.Draw(inRect);
    public override void WriteSettings()
    {
        Settings.Sanitize();
        base.WriteSettings();
        CounterOverlay.Reset();
    }
}
