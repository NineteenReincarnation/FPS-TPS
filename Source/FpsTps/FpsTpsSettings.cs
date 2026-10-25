using Verse;

namespace Anno.FpsTps;

public sealed class FpsTpsSettings : ModSettings
{
    public bool ShowFps = true;
    public bool ShowTps = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ShowFps, "showFps", true);
        Scribe_Values.Look(ref ShowTps, "showTps", true);
    }
}
