namespace Anno.FpsTps;

/// <summary>Actual completed ticks per wall-clock second. No hooks on the simulation loop.</summary>
internal struct RateSampler
{
    private bool initialized;
    private bool wasPaused;
    private double startTime;
    private double lastTime;
    private int startTicks;
    private int lastTicks;
    private float previousTarget;

    public double Value { get; private set; }
    public bool Ready { get; private set; }

    public bool Advance(double now, int ticks, bool paused, float target, double interval)
    {
        bool discontinuity = !initialized || paused != wasPaused || target != previousTarget
            || ticks < lastTicks || now < lastTime || now - lastTime > Math.Max(2d, interval * 3d);
        lastTime = now;
        lastTicks = ticks;
        previousTarget = target;
        wasPaused = paused;
        if (discontinuity)
        {
            initialized = true;
            startTime = now;
            startTicks = ticks;
            Value = 0;
            Ready = paused;
            return true;
        }
        if (paused || now - startTime < interval)
            return false;

        Value = Math.Max(0d, ((long)ticks - startTicks) / (now - startTime));
        Ready = true;
        startTime = now;
        startTicks = ticks;
        return true;
    }
}
