using System.Globalization;
using System.Text;

namespace KsFit.Core;

/// <summary>
/// Appends one CSV row per treadmill sample or event to a per-connection session file.
/// Columns: timestamp_utc,elapsed_s,speed_kmh,distance_m,kcal,state,target_kmh,event
/// </summary>
public sealed class SessionLogger : IDisposable
{
    public const string Header = "timestamp_utc,elapsed_s,speed_kmh,distance_m,kcal,state,target_kmh,event";
    readonly StreamWriter _w;
    readonly object _lock = new();
    public string Path { get; }

    public SessionLogger(string directory)
    {
        Directory.CreateDirectory(directory);
        Path = System.IO.Path.Combine(directory, $"session-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        _w = new StreamWriter(new FileStream(Path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
        _w.WriteLine(Header);
        _w.Flush();
    }

    static string F(double? v, string f) => v?.ToString(f, CultureInfo.InvariantCulture) ?? "";

    public void Sample(TreadmillData d, MachineState state, double target) =>
        Write(d.Elapsed?.TotalSeconds, d.SpeedKmh, d.DistanceM, d.Calories, state, target, "");

    public void Event(string name, MachineState state, double target) =>
        Write(null, null, null, null, state, target, name);

    void Write(double? elapsed, double? speed, double? dist, double? kcal, MachineState state, double target, string ev)
    {
        var line = string.Join(',', DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            F(elapsed, "0"), F(speed, "0.00"), F(dist, "0"), F(kcal, "0"), state, F(target, "0.0"), ev);
        lock (_lock) { try { _w.WriteLine(line); _w.Flush(); } catch (ObjectDisposedException) { } }
    }

    public void Dispose() { lock (_lock) _w.Dispose(); }
}
