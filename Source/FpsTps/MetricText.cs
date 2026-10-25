using System.Globalization;

namespace Anno.FpsTps;

/// <summary>Formats the two standard counters only when their displayed numbers change.</summary>
internal sealed class MetricText
{
    private (bool, double, double) previous;
    private bool initialized;
    public string Text { get; private set; } = "";
    public int Revision { get; private set; }

    public void Update(bool fps, double value, double target = 0)
    {
        double rounded = double.IsFinite(value) ? Math.Round(Math.Max(0, value)) : 0;
        double roundedTarget = !fps && double.IsFinite(target) ? Math.Round(Math.Max(0, target)) : 0;
        var key = (fps, rounded, roundedTarget);
        if (initialized && previous == key)
            return;
        initialized = true;
        previous = key;
        Text = fps
            ? string.Create(CultureInfo.InvariantCulture, $"FPS: {rounded:F0}")
            : string.Create(CultureInfo.InvariantCulture, $"TPS: {rounded:F0} ({roundedTarget:F0})");
        Revision++;
    }
}
