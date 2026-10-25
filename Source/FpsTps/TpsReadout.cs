using System.Runtime.CompilerServices;

namespace Anno.FpsTps;

/// <summary>Completed simulation ticks divided by elapsed wall time; never estimates from tick cost.</summary>
internal struct TpsReadout
{
    private const int Capacity = 128;
    private const int Mask = Capacity - 1;
    private const double Window = 0.25;
    private const double HistoryStep = Window / (Capacity - 2);

    private struct Sample
    {
        public double Time;
        public int Ticks;
    }

    [InlineArray(Capacity)]
    private struct Samples
    {
        private Sample first;
    }

    private Samples history;
    private int head;
    private int count;
    private bool initialized;
    private bool wasPaused;
    private int previousSpeed;
    private int lastTicks;
    private double lastTime;
    private double lastStoredTime;
    private double startedAt;
    private double nextRefresh;

    public double Value { get; private set; }
    public double Target { get; private set; }

    public bool Advance(double now, int ticks, int speed, bool paused, double target, double interval)
    {
        if (!double.IsFinite(now))
            return false;
        double newTarget = paused || !double.IsFinite(target) ? 0 : Math.Max(0, target);
        bool newTimeline = !initialized || now < lastTime || ticks < lastTicks;
        bool changed = newTimeline || speed != previousSpeed || paused != wasPaused || newTarget != Target;
        double elapsedFrame = now - lastTime;
        int previousTicks = lastTicks;
        previousSpeed = speed;
        wasPaused = paused;
        lastTime = now;
        lastTicks = ticks;
        Target = newTarget;
        if (changed)
        {
            initialized = true;
            BeginWindow(now, ticks);
            // During a speed change retain only the last genuinely measured value until the
            // very next frame supplies a new tick delta. Never substitute the requested target.
            if (newTimeline || paused)
                Value = 0;
            return true;
        }
        if (paused || elapsedFrame <= 0)
            return false;
        if (elapsedFrame > Window)
        {
            // A long frame/loading gap is itself a real observation, including its waiting time.
            Value = Math.Max(0, ((long)ticks - previousTicks) / elapsedFrame);
            BeginWindow(now, ticks);
            return true;
        }

        double cutoff = now - Window;
        while (count > 1 && history[(head + 1) & Mask].Time <= cutoff)
        {
            head = (head + 1) & Mask;
            count--;
        }
        bool publish = now >= nextRefresh;
        if (publish)
        {
            Sample baseline = history[head];
            double elapsed = now - baseline.Time;
            if (elapsed > 0)
                Value = Math.Max(0, ((long)ticks - baseline.Ticks) / elapsed);
            double regularInterval = double.IsFinite(interval) && interval > 0 ? interval : 1;
            // First post-change frame always publishes; short refreshes continue while the
            // rolling window fills, then the user's regular display interval applies.
            nextRefresh = now + (now - startedAt < Window ? Math.Min(0.1, regularInterval) : regularInterval);
        }
        if (now - lastStoredTime >= HistoryStep)
        {
            if (count == Capacity)
            {
                head = (head + 1) & Mask;
                count--;
            }
            history[(head + count) & Mask] = new Sample { Time = now, Ticks = ticks };
            count++;
            lastStoredTime = now;
        }
        return publish;
    }

    private void BeginWindow(double now, int ticks)
    {
        head = 0;
        count = 1;
        history[0] = new Sample { Time = now, Ticks = ticks };
        lastStoredTime = startedAt = nextRefresh = now;
    }
}
