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
    private (double, double, double, double, int, MetricParts, int) previous;
    private bool initialized;
    public string Text { get; private set; } = "";
    public int Revision { get; private set; }

    public void Update(bool fps, double value, double target, double cost, int decimals,
        MetricParts parts, int state, string pausedLabel)
    {
        decimals = Math.Clamp(decimals, 0, 2);
        double rounded = Math.Round(double.IsFinite(value) ? value : 0, decimals);
        double roundedTarget = ((parts & MetricParts.Target) != 0) || ((parts & MetricParts.Percent) != 0)
            ? Math.Round(double.IsFinite(target) ? target : 0, 1) : 0;
        double roundedCost = ((parts & MetricParts.Cost) != 0) ? Math.Round(double.IsFinite(cost) ? cost : 0, 2) : 0;
        double percent = ((parts & MetricParts.Percent) != 0) && target > 0
            ? Math.Round(value / target * 100) : 0;
        var key = (rounded, roundedTarget, roundedCost, percent, decimals, parts, state);
        if (initialized && previous == key)
            return;
        initialized = true;
        previous = key;
        builder.Clear().Append(fps ? "FPS: " : "TPS: ");
        if (state == 1)
            builder.Append(pausedLabel);
        else if (state == 2)
            builder.Append('\u2014');
        else
            builder.Append(rounded.ToString(Formats[decimals], CultureInfo.InvariantCulture));
        if (!fps && state != 1 && ((parts & MetricParts.Target) != 0))
            builder.Append(" (").Append(roundedTarget.ToString("F0", CultureInfo.InvariantCulture)).Append(')');
        if (!fps && state == 0 && ((parts & MetricParts.Percent) != 0) && target > 0)
            builder.Append("  ").Append(percent.ToString("F0", CultureInfo.InvariantCulture)).Append('%');
        if (state == 0 && ((parts & MetricParts.Cost) != 0))
            builder.Append("  ").Append(roundedCost.ToString("F2", CultureInfo.InvariantCulture)).Append(fps ? " ms/f" : " ms/t");
        Text = builder.ToString();
        Revision++;
    }
}
