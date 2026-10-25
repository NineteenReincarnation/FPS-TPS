namespace Anno.FpsTps;

/// <summary>Native-style TPS, with an immediate refresh when selected speed or pause changes.</summary>
internal struct TpsReadout
{
    private bool initialized;
    private bool wasPaused;
    private int previousSpeed;
    private double updatedAt;
    private double nextRefresh;

    public double Value { get; private set; }
    public double Target { get; private set; }
    public double TickTime { get; private set; }

    public readonly bool NeedsRefresh(double now, int speed, bool paused) =>
        !initialized || speed != previousSpeed || paused != wasPaused || now >= nextRefresh || now < updatedAt;

    public void Update(double now, int speed, bool paused, double target, double tickTime, double interval)
    {
        initialized = true;
        previousSpeed = speed;
        wasPaused = paused;
        updatedAt = now;
        nextRefresh = now + interval;
        Target = paused || !double.IsFinite(target) ? 0 : Math.Max(0, target);
        TickTime = paused || !double.IsFinite(tickTime) ? 0 : Math.Max(0, tickTime);
        // Same basis as RimWorld's native TPS counter. Zero timing is its initial state;
        // division tends to infinity, so the current target is displayed immediately.
        Value = Target == 0 || !double.IsFinite(tickTime) || tickTime < 0 ? 0
            : TickTime == 0 ? Target : Math.Min(1000d / TickTime, Target);
    }
}
