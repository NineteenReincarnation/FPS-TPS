using System.Diagnostics;
using System.Globalization;
using Anno.FpsTps;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
    checks++;
    Console.WriteLine("PASS " + name);
}

var sampler = new RateSampler();
Check(sampler.Advance(0, 0, false, 60, 1) && !sampler.Ready, "Initial sample waits for a full window");
Check(!sampler.Advance(0.5, 30, false, 60, 1), "Configured window is respected");
sampler.Advance(1, 60, false, 60, 1);
Check(sampler.Ready && Math.Abs(sampler.Value - 60) < 0.001, "Normal speed actual TPS");
sampler.Advance(2, 90, false, 60, 1);
Check(Math.Abs(sampler.Value - 30) < 0.001, "Actual throughput is not inferred from tick cost");
sampler.Advance(2.1, 96, true, 0, 1);
Check(sampler.Ready && sampler.Value == 0, "Pause immediately clears stale TPS");
sampler.Advance(10, 96, true, 0, 1);
sampler.Advance(11, 96, false, 180, 1);
Check(!sampler.Ready, "Resume discards time spent paused");
sampler.Advance(12, 276, false, 180, 1);
Check(Math.Abs(sampler.Value - 180) < 0.001, "Fast speed TPS");
sampler.Advance(12.1, 294, false, 360, 1);
Check(!sampler.Ready, "Speed change discards mixed-speed window");
Check(sampler.Value == 180, "Speed change retains the previous numeric reading while resampling");
sampler.Advance(13.1, 654, false, 360, 1);
Check(Math.Abs(sampler.Value - 360) < 0.001, "Superfast speed TPS");
sampler.Advance(13.2, 1, false, 360, 1);
Check(!sampler.Ready, "Tick rewind / save switch resets measurement");
sampler.Advance(25, 400, false, 360, 1);
Check(!sampler.Ready, "Hidden UI or long loading gap resets measurement");
sampler.Advance(26, 700, false, 360, 1);
Check(Math.Abs(sampler.Value - 300) < 0.001, "Recovers after loading gap");
sampler.Advance(25, 800, false, 360, 1);
Check(!sampler.Ready, "Clock discontinuity resets measurement");

var text = new MetricText();
text.Update(false, 59.5, 60, 0.62, 1, MetricParts.Target | MetricParts.Percent | MetricParts.Cost);
Check(text.Text == "TPS: 59.5 (60)  99%  0.62 ms/t", "Detailed TPS formatting");
int revision = text.Revision;
text.Update(false, 59.501, 60, 0.6201, 1, MetricParts.Target | MetricParts.Percent | MetricParts.Cost);
Check(revision == text.Revision, "Text cache ignores invisible changes");
text.Update(false, 177.2, 180, 0, 0, MetricParts.Percent);
text.Update(false, 177.4, 180, 0, 0, MetricParts.Percent);
Check(text.Text == "TPS: 177  99%", "Percentage refreshes even when rounded TPS is unchanged");
text.Update(false, 0, 0, 0, 0, MetricParts.Target | MetricParts.Percent | MetricParts.Cost, true);
Check(text.Text == "TPS: 0 (0)", "Pause displays numeric zero and zero target without stale details");
text.Update(false, 180, 180, 1.2, 0, MetricParts.Target, true);
Check(text.Text == "TPS: 0 (0)", "Paused formatting cannot leak a previous nonzero reading");
text.Update(false, 0, 0, 0, 0, MetricParts.None, true);
Check(text.Text == "TPS: 0", "Pause remains numeric when target display is disabled");
text.Update(false, 0, 180, 0, 0, MetricParts.Target);
Check(text.Text == "TPS: 0 (180)", "Initial numeric display has no placeholder marker");
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
text.Update(true, 60.15, 0, 16.62, 2, MetricParts.Cost);
Check(text.Text == "FPS: 60.15  16.62 ms/f", "Stable invariant numeric display");
text.Update(true, double.NaN, 0, double.PositiveInfinity, 0, MetricParts.Cost);
Check(!text.Text.Contains("NaN") && !text.Text.Contains("Infinity"), "Invalid native timing values are sanitized");

// Measure the real linked implementation, not copies. UI/game rendering is deliberately excluded.
const int iterations = 2_000_000;
sampler = default;
for (int i = 0; i < 100_000; i++) sampler.Advance(i / 120d, i / 2, false, 60, 1);
GC.Collect();
long allocated = GC.GetAllocatedBytesForCurrentThread();
long started = Stopwatch.GetTimestamp();
for (int i = 100_000; i < iterations + 100_000; i++) sampler.Advance(i / 120d, i / 2, false, 60, 1);
double samplerNs = Stopwatch.GetElapsedTime(started).TotalNanoseconds / iterations;
long samplerBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
text.Update(true, 60, 0, 16.67, 0, MetricParts.Cost);
for (int i = 0; i < 100_000; i++) text.Update(true, 60, 0, 16.67, 0, MetricParts.Cost);
allocated = GC.GetAllocatedBytesForCurrentThread();
started = Stopwatch.GetTimestamp();
for (int i = 0; i < iterations; i++) text.Update(true, 60, 0, 16.67, 0, MetricParts.Cost);
double cacheNs = Stopwatch.GetElapsedTime(started).TotalNanoseconds / iterations;
long cacheBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
Check(samplerBytes == 0, "Sampler steady-state allocations = 0 bytes");
Check(cacheBytes == 0, "Unchanged text cache allocations = 0 bytes");
Console.WriteLine($"RESULT {checks} checks passed.");
Console.WriteLine(FormattableString.Invariant($"BENCH sampler: {samplerNs:F2} ns/call; {samplerBytes} bytes / {iterations:N0} calls"));
Console.WriteLine(FormattableString.Invariant($"BENCH unchanged text: {cacheNs:F2} ns/call; {cacheBytes} bytes / {iterations:N0} calls"));
Console.WriteLine("These are core microbenchmarks, not an in-game FPS overhead measurement.");
