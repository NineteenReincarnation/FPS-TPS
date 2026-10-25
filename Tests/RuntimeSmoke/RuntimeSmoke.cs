using System.Reflection;
using Anno.FpsTps;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Anno.FpsTps.RuntimeSmoke;

/// <summary>Optional headless integration test DLL, never included in release packages.</summary>
[StaticConstructorOnStartup]
internal static class RuntimeSmoke
{
    static RuntimeSmoke()
    {
        if (GenCommandLine.CommandLineArgPassed("FpsTpsSmokeTest"))
            LongEventHandler.ExecuteWhenFinished(Run);
    }

    private static void Run()
    {
        List<string> results = [];
        void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException(name);
            results.Add("PASS " + name);
        }
        Game? previousGame = Current.Game;
        ProgramState previousState = Current.ProgramState;
        Event previousEvent = Event.current;
        FieldInfo settingsField = typeof(FpsTpsMod).GetField("Settings", BindingFlags.NonPublic | BindingFlags.Static)!;
        var originalSettings = (FpsTpsSettings)settingsField.GetValue(null)!;
        try
        {
            Check(LoadedModManager.GetMod<FpsTpsMod>() != null, "Mod instantiated by actual RimWorld 1.7 runtime");
            MethodBase[] patched = Harmony.GetAllPatchedMethods()
                .Where(m => Harmony.GetPatchInfo(m)?.Owners.Contains("anno.fpsandtps") == true).ToArray();
            Check(patched.Length == 3 && patched.All(m => m.DeclaringType == typeof(GlobalControlsUtility)), "Exactly three UI-only patches; no tick or Update hooks");
            Check("AT_Paused".Translate().ToString() == "已暂停", "Chinese translation loaded by game");

            FpsTpsSettings s = new() { FpsInterval = 1.3f, TpsInterval = 2.4f, Width = 370f, ShowPercent = true, Colorize = true, Palette = 2 };
            string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "settings-roundtrip.xml");
            Scribe.saver.InitSaving(path, "FpsTpsSmoke");
            Scribe_Deep.Look(ref s, "settings");
            Scribe.saver.FinalizeSaving();
            FpsTpsSettings? loaded = null;
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref loaded, "settings");
            Scribe.loader.FinalizeLoading();
            FieldInfo[] fields = typeof(FpsTpsSettings).GetFields(BindingFlags.Instance | BindingFlags.Public);
            Check(loaded != null && fields.All(f => Equals(f.GetValue(s), f.GetValue(loaded))), $"All {fields.Length} setting fields survive a native Scribe round trip");
            s.FpsInterval = float.NaN;
            s.Width = float.PositiveInfinity;
            s.WarningFps = 999f;
            s.Palette = 999;
            s.Sanitize();
            Check(s.FpsInterval == 0.5f && s.Width == 240f && s.WarningFps == s.GoodFps && s.Palette == 2, "Malformed settings are bounded and finite");

            s = new FpsTpsSettings();
            settingsField.SetValue(null, s);
            Current.Game = new Game();
            Current.ProgramState = ProgramState.Playing;
            Event.current = new Event { type = EventType.mouseDown };
            Type overlay = typeof(FpsTpsMod).Assembly.GetType("Anno.FpsTps.CounterOverlay")!;
            Check(overlay.GetField("manager", BindingFlags.NonPublic | BindingFlags.Static)!.FieldType
                == typeof(System.WeakReference<TickManager>), "UI cache does not strongly retain a closed colony's tick manager");
            MethodInfo draw = overlay.GetMethod("Draw", BindingFlags.NonPublic | BindingFlags.Static)!;
            MethodInfo replaces = overlay.GetMethod("ReplacesVanilla", BindingFlags.NonPublic | BindingFlags.Static)!;
            float Reserve()
            {
                object[] args = [1000f, 200f, 500f];
                draw.Invoke(null, args);
                return 500f - (float)args[2];
            }
            Check(Reserve() == 52f, "Two native rows reserve 52 UI units on input events");
            s.Horizontal = true;
            Check(Reserve() == 26f, "Horizontal layout reserves one row");
            s.Gap = 5;
            Check(Reserve() == 31f, "Custom gap is reserved rather than overlapping controls");
            Check(overlay.GetField("manager", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null) == null,
                "Input events do not sample game metrics");
            Event.current = new Event { type = EventType.repaint };
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = true;
            GUI.color = Color.cyan;
            Reserve();
            Check(Text.Font == GameFont.Medium && Text.Anchor == TextAnchor.MiddleLeft
                && Text.WordWrap && GUI.color == Color.cyan, "Repaint restores native font, alignment, wrapping and color");
            Event.current = new Event { type = EventType.mouseDown };
            Check((bool)replaces.Invoke(null, [true])! && (bool)replaces.Invoke(null, [false])!, "Visible metrics replace vanilla debug duplicates");
            float y = 500;
            GlobalControlsUtility.DrawFpsCounter(1000, 200, ref y);
            GlobalControlsUtility.DrawTpsCounter(1000, 200, ref y);
            Check(y == 500, "Actual Harmony prefixes skip duplicate native drawing and height");
            s.ShowTps = false;
            Check(!(bool)replaces.Invoke(null, [false])!, "Hidden TPS does not suppress vanilla TPS");
            s.Enabled = false;
            Check(Reserve() == 0 && !(bool)replaces.Invoke(null, [true])!, "Disabled mod reserves no space and leaves vanilla counters available");
            s.Enabled = true;
            s.HidePaused = true;
            typeof(TickManager).GetField("curTimeSpeed", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(Find.TickManager, TimeSpeed.Paused);
            Check(Reserve() == 0 && !(bool)replaces.Invoke(null, [true])!, "Pause visibility and duplicate handling agree");
            Check(overlay.GetField("manager", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null) == null,
                "Hidden counters discard prior sampling state, including short hidden pauses");
            s.HidePaused = false;
            Current.ProgramState = ProgramState.Entry;
            Check(Reserve() == 0, "Main menu has no counters");
            results.Add($"RESULT {results.Count} runtime checks passed. Native UI pixels were not inspected.");
            Log.Message("[FPS & TPS smoke] All runtime integration checks passed.");
        }
        catch (Exception ex)
        {
            results.Add("FAIL " + ex);
            Log.Error("[FPS & TPS smoke] " + ex);
        }
        finally
        {
            settingsField.SetValue(null, originalSettings);
            Current.Game = previousGame;
            Current.ProgramState = previousState;
            Event.current = previousEvent;
            File.WriteAllLines(Path.Combine(GenFilePaths.SaveDataFolderPath, "runtime-smoke-results.txt"), results);
        }
    }
}
