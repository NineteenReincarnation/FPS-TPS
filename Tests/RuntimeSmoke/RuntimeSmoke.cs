using System.Reflection;
using Anno.FpsTps;
using RimWorld;
using UnityEngine;
using Verse;

namespace Anno.FpsTps.RuntimeSmoke;

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
        UIRoot? previousUiRoot = Current.Root.uiRoot;
        ProgramState previousState = Current.ProgramState;
        Event previousEvent = Event.current;
        bool previousFps = DebugViewSettings.showFpsCounter;
        bool previousTps = DebugViewSettings.showTpsCounter;
        FieldInfo settingsField = typeof(FpsTpsMod).GetField("Settings", BindingFlags.NonPublic | BindingFlags.Static)!;
        var originalSettings = (FpsTpsSettings)settingsField.GetValue(null)!;
        Type assemblyType = typeof(FpsTpsMod);
        Type overlay = assemblyType.Assembly.GetType("Anno.FpsTps.CounterOverlay")!;
        Type guard = assemblyType.Assembly.GetType("Anno.FpsTps.NativeCounterVisibility")!;
        MethodInfo restore = guard.GetMethod("Restore", BindingFlags.NonPublic | BindingFlags.Static)!;
        Window? ownedWindow = null;
        try
        {
            Check(LoadedModManager.GetMod<FpsTpsMod>() != null, "Game loads Mod without prerequisite mods");
            Check(ModsConfig.ActiveModsInLoadOrder.Count() == 2, "Isolated active Mods are Core and FPS & TPS only");
            Check(!assemblyType.Assembly.GetReferencedAssemblies().Any(a => a.Name!.Contains("Harmony", StringComparison.OrdinalIgnoreCase)), "Compiled Mod has no Harmony reference");
            Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name!.Contains("Harmony", StringComparison.OrdinalIgnoreCase)), "No Harmony assembly is loaded in the running game");
            Check("AT_Display".Translate().ToString() == "显示", "Chinese translations load without a prerequisite");
            Check(typeof(FpsTpsGameComponent).GetMethod("GameComponentTick")!.DeclaringType == typeof(GameComponent)
                && typeof(FpsTpsGameComponent).GetMethod("GameComponentUpdate")!.DeclaringType == typeof(GameComponent), "No simulation tick or Update override");

            FpsTpsSettings s = new() { FpsInterval = 1.3f, TpsInterval = 5f, Width = 370f, ShowPercent = true, Colorize = true, Palette = 2 };
            string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "settings-roundtrip.xml");
            Scribe.saver.InitSaving(path, "FpsTpsSmoke");
            Scribe_Deep.Look(ref s, "settings");
            Scribe.saver.FinalizeSaving();
            FpsTpsSettings? loaded = null;
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref loaded, "settings");
            Scribe.loader.FinalizeLoading();
            FieldInfo[] fields = typeof(FpsTpsSettings).GetFields(BindingFlags.Instance | BindingFlags.Public);
            Check(loaded != null && fields.All(f => Equals(f.GetValue(s), f.GetValue(loaded))), $"All {fields.Length} settings survive native serialization");
            s.FpsInterval = float.NaN;
            s.Width = float.PositiveInfinity;
            s.WarningFps = 999;
            s.Palette = 999;
            s.Sanitize();
            Check(s.FpsInterval == 0.5f && s.Width == 240f && s.WarningFps == s.GoodFps && s.Palette == 2, "Malformed settings are bounded and finite");

            s = new FpsTpsSettings { TpsInterval = 5 };
            settingsField.SetValue(null, s);
            Current.Game = new Game();
            Current.ProgramState = ProgramState.Playing;
            // Startup self-tests run before Root.Start has created the UI; supply the same
            // native play UI class here and restore it before normal startup continues.
            Current.Root.uiRoot = new UIRoot_Play();
            var component = Current.Game.GetComponent<FpsTpsGameComponent>();
            Check(component != null, "Game automatically creates its native GUI component");
            Event.current = new Event { type = EventType.mouseDown };
            component!.GameComponentOnGUI();
            ownedWindow = (Window)typeof(FpsTpsGameComponent).GetField("window", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(component)!;
            component.GameComponentOnGUI();
            Check(Find.WindowStack.Windows.Count(w => w.GetType() == ownedWindow.GetType()) == 1, "Repeated GUI passes keep a single overlay window");
            Check(ownedWindow.layer == WindowLayer.GameUI && ownedWindow.windowRect.width == 0
                && !ownedWindow.focusWhenOpened && !ownedWindow.absorbInputAroundWindow
                && !ownedWindow.preventCameraMotion && !ownedWindow.forcePause && !ownedWindow.preventSave,
                "Overlay has no hit area and does not take focus, pause, block saving, or block the camera");
            ownedWindow.OnCancelKeyPressed();
            ownedWindow.OnAcceptKeyPressed();
            Check(Find.WindowStack.IsOpen(ownedWindow) && Event.current.type == EventType.mouseDown,
                "Overlay does not consume Escape or Enter");
            Check(overlay.GetField("manager", BindingFlags.NonPublic | BindingFlags.Static)!.FieldType
                == typeof(System.WeakReference<TickManager>), "UI cache does not strongly retain a closed colony");
            MethodInfo draw = overlay.GetMethod("Draw", BindingFlags.NonPublic | BindingFlags.Static)!;
            float Reserve()
            {
                object[] args = [1000f, 200f, 500f];
                draw.Invoke(null, args);
                return 500f - (float)args[2];
            }
            Check(Reserve() == 52, "Vertical layout height remains 52 UI units");
            s.Horizontal = true;
            Check(Reserve() == 26, "Horizontal layout height remains one native row");
            s.Gap = 5;
            Check(Reserve() == 31, "Custom spacing is preserved");
            Check(overlay.GetField("manager", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null) == null,
                "Input events do not collect metrics");

            MethodInfo suppress = guard.GetMethod("Suppress", BindingFlags.NonPublic | BindingFlags.Static)!;
            DebugViewSettings.showFpsCounter = DebugViewSettings.showTpsCounter = true;
            suppress.Invoke(null, [s]);
            Check(!DebugViewSettings.showFpsCounter && !DebugViewSettings.showTpsCounter, "Native duplicate flags are suppressed during native drawing only");
            restore.Invoke(null, null);
            Check(DebugViewSettings.showFpsCounter && DebugViewSettings.showTpsCounter, "Original native debug preferences are restored");
            s.ShowTps = false;
            suppress.Invoke(null, [s]);
            Check(!DebugViewSettings.showFpsCounter && DebugViewSettings.showTpsCounter, "Hidden metric does not hide the native equivalent");
            restore.Invoke(null, null);
            s.ShowTps = true;
            Event.current = new Event { type = EventType.repaint };
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = true;
            GUI.color = Color.cyan;
            Reserve();
            Check(Text.Font == GameFont.Medium && Text.Anchor == TextAnchor.MiddleLeft
                && Text.WordWrap && GUI.color == Color.cyan, "Repaint restores native font, alignment, wrap, and color");
            suppress.Invoke(null, [s]);
            ownedWindow.WindowOnGUI();
            Check(DebugViewSettings.showFpsCounter && DebugViewSettings.showTpsCounter, "Late native GUI callback restores duplicate flags after drawing");

            s.Enabled = false;
            Check(Reserve() == 0, "Disabled overlay takes no display space");
            s.Enabled = true;
            s.HidePaused = true;
            var tickManager = Find.TickManager;
            FieldInfo speedField = typeof(TickManager).GetField("curTimeSpeed", BindingFlags.Instance | BindingFlags.NonPublic)!;
            speedField.SetValue(tickManager, TimeSpeed.Paused);
            Check(Reserve() == 0, "Hide-on-pause remains functional");
            s.HidePaused = false;
            MethodInfo refresh = overlay.GetMethod("Refresh", BindingFlags.NonPublic | BindingFlags.Static)!;
            object tpsText = overlay.GetField("TpsText", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            string TextValue() => (string)tpsText.GetType().GetProperty("Text")!.GetValue(tpsText)!;
            refresh.Invoke(null, [s]);
            Check(TextValue() == "TPS: 0 (0)", "Paused display is immediately numeric 0 (0)");
            foreach (TimeSpeed speed in new[] { TimeSpeed.Normal, TimeSpeed.Fast, TimeSpeed.Superfast, TimeSpeed.Normal })
            {
                speedField.SetValue(tickManager, speed);
                refresh.Invoke(null, [s]);
                float target = tickManager.Paused ? 0 : tickManager.TickRateMultiplier * 60;
                double expected = tickManager.Paused ? 0 : tickManager.MeanTickTime > 0
                    ? Math.Min(1000d / tickManager.MeanTickTime, target) : target;
                Check(TextValue() == $"TPS: {expected:F0} ({target:F0})", $"Native {speed} change refreshes immediately despite five-second interval");
            }
            Check(typeof(FpsTpsSettings).GetField("Tooltips") == null, "Overlay hover explanation stays removed");
            Current.ProgramState = ProgramState.Entry;
            Check(Reserve() == 0, "Main menu displays no counters");
            results.Add($"RESULT {results.Count} runtime checks passed. Visual pixels were not inspected.");
            Log.Message("[FPS & TPS smoke] Dependency-free native GUI checks passed.");
        }
        catch (Exception ex)
        {
            results.Add("FAIL " + ex);
            Log.Error("[FPS & TPS smoke] " + ex);
        }
        finally
        {
            restore.Invoke(null, null);
            if (ownedWindow != null && Find.WindowStack.IsOpen(ownedWindow)) Find.WindowStack.TryRemove(ownedWindow, false);
            DebugViewSettings.showFpsCounter = previousFps;
            DebugViewSettings.showTpsCounter = previousTps;
            settingsField.SetValue(null, originalSettings);
            Current.Game = previousGame;
            Current.Root.uiRoot = previousUiRoot;
            Current.ProgramState = previousState;
            Event.current = previousEvent;
            File.WriteAllLines(Path.Combine(GenFilePaths.SaveDataFolderPath, "runtime-smoke-results.txt"), results);
        }
    }
}
