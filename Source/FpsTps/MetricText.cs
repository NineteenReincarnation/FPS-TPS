using System.Globalization;
using System.Text;

namespace Anno.FpsTps;

[Flags]
internal enum MetricParts { None = 0, Target = 1, Percent = 2, Cost = 4 }

/// <summary>Formats only when displayed values or options change; unchanged calls allocate nothing.</summary>
internal sealed class MetricText
{
    private static readonly string[] Formats = ["F0", "F1", "F2"];
    private readonly StringBuilder builder = new(128);
    private (double, double, double, double, int, MetricParts, bool) previous;
    private bool initialized;
    public string Text { get; private set; } = "";
    public int Revision { get; private set; }

    public void Update(bool fps, double value, double target, double cost, int decimals,
        MetricParts parts, bool paused = false)
    {
        decimals = Math.Clamp(decimals, 0, 2);
        value = paused || !double.IsFinite(value) ? 0 : value;
        target = paused || !double.IsFinite(target) ? 0 : target;
        double rounded = Math.Round(value, decimals);
        double roundedTarget = ((parts & MetricParts.Target) != 0) || ((parts & MetricParts.Percent) != 0)
            ? Math.Round(target, 1) : 0;
        double roundedCost = !paused && ((parts & MetricParts.Cost) != 0) ? Math.Round(double.IsFinite(cost) ? cost : 0, 2) : 0;
        double percent = ((parts & MetricParts.Percent) != 0) && target > 0
            ? Math.Round(value / target * 100) : 0;
        var key = (rounded, roundedTarget, roundedCost, percent, decimals, parts, paused);
        if (initialized && previous == key)
            return;
        initialized = true;
        previous = key;
        builder.Clear().Append(fps ? "FPS: " : "TPS: ");
        builder.Append(rounded.ToString(Formats[decimals], CultureInfo.InvariantCulture));
        if (!fps && ((parts & MetricParts.Target) != 0))
            builder.Append(" (").Append(roundedTarget.ToString("F0", CultureInfo.InvariantCulture)).Append(')');
        if (!fps && !paused && ((parts & MetricParts.Percent) != 0) && target > 0)
            builder.Append("  ").Append(percent.ToString("F0", CultureInfo.InvariantCulture)).Append('%');
        if (!paused && ((parts & MetricParts.Cost) != 0))
            builder.Append("  ").Append(roundedCost.ToString("F2", CultureInfo.InvariantCulture)).Append(fps ? " ms/f" : " ms/t");
        Text = builder.ToString();
        Revision++;
    }
}
