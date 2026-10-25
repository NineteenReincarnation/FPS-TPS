using HarmonyLib;
using RimWorld;
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
        new Harmony("anno.fpsandtps").PatchAll(typeof(FpsTpsMod).Assembly);
        Log.Message("[FPS & TPS] anno.fpsandtps 1.1.1 loaded.");
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

[HarmonyPatch(typeof(GlobalControlsUtility), nameof(GlobalControlsUtility.DoTimespeedControls))]
internal static class NativeControlsPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(float leftX, float width, ref float curBaseY) =>
        CounterOverlay.Draw(leftX, width, ref curBaseY);
}

[HarmonyPatch(typeof(GlobalControlsUtility), nameof(GlobalControlsUtility.DrawFpsCounter))]
internal static class VanillaFpsPatch
{
    [HarmonyPrefix]
    private static bool Prefix() => !CounterOverlay.ReplacesVanilla(true);
}

[HarmonyPatch(typeof(GlobalControlsUtility), nameof(GlobalControlsUtility.DrawTpsCounter))]
internal static class VanillaTpsPatch
{
    [HarmonyPrefix]
    private static bool Prefix() => !CounterOverlay.ReplacesVanilla(false);
}
