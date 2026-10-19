using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Avalonia.Threading;

namespace KsFit.App;

public sealed record SessionSummary(
    string File, string Path, DateTime Started, TimeSpan Duration, double DistanceKm, double AvgSpeed, double MaxSpeed, int Calories)
{
    public string StartedText => Started.ToString("yyyy-MM-dd HH:mm");
    public string DurationText => Duration.TotalHours >= 1 ? Duration.ToString(@"h\:mm\:ss") : Duration.ToString(@"mm\:ss");
}

public sealed record SampleRow(string Time, string Elapsed, string Speed, string Distance, string Calories, string State, string Target, string Event);

/// <summary>Reads the CSV session logs written by <see cref="KsFit.Core.SessionLogger"/>.</summary>
public sealed class SessionBrowserViewModel : Observable
{
    public ObservableCollection<SessionSummary> Sessions { get; } = new();
    public ObservableCollection<SampleRow> Samples { get; } = new();

    bool _expanded;
    public bool IsExpanded { get => _expanded; set { if (Set(ref _expanded, value) && value) _ = RefreshAsync(); } }

    SessionSummary? _selected;
    public SessionSummary? Selected { get => _selected; set { if (Set(ref _selected, value)) _ = LoadSamplesAsync(value); } }

    string _info = "";
    public string Info { get => _info; private set => Set(ref _info, value); }

    public ICommand RefreshCmd { get; }
    public ICommand OpenFolderCmd { get; }

    public SessionBrowserViewModel(string directory)
    {
        _dir = directory;
        RefreshCmd = new AsyncCommand(RefreshAsync, e => Info = e.Message);
        OpenFolderCmd = new AsyncCommand(() =>
        {
            Directory.CreateDirectory(_dir);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_dir) { UseShellExecute = true });
            return Task.CompletedTask;
        }, e => Info = e.Message);
    }

    readonly string _dir;

    public async Task RefreshAsync()
    {
        var keep = _selected?.Path;
        var list = await Task.Run(() =>
        {
            var res = new List<SessionSummary>();
            if (!Directory.Exists(_dir)) return res;
            foreach (var f in Directory.GetFiles(_dir, "session-*.csv"))
                try { res.Add(Summarize(f)); } catch { }
            return res.OrderByDescending(s => s.Started).ToList();
        });
        Sessions.Clear();
        foreach (var s in list) Sessions.Add(s);
        Info = list.Count == 0 ? "No sessions yet. Connect to a pad to start logging." : $"{list.Count} session(s)";
        Selected = Sessions.FirstOrDefault(s => s.Path == keep);
    }

    static IEnumerable<string[]> Rows(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var r = new StreamReader(fs);
        r.ReadLine(); // header
        string? line;
        while ((line = r.ReadLine()) != null)
        {
            var c = line.Split(',');
            if (c.Length >= 8) yield return c;
        }
    }

    static double D(string s) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

    static SessionSummary Summarize(string path)
    {
        DateTime start = default; double maxElapsed = 0, dist = 0, max = 0, sum = 0; int n = 0, kcal = 0;
        foreach (var c in Rows(path))
        {
            if (start == default && DateTime.TryParse(c[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var t))
                start = t.ToLocalTime();
            if (c[2].Length == 0) continue; // event row
            var sp = D(c[2]);
            maxElapsed = Math.Max(maxElapsed, D(c[1]));
            dist = Math.Max(dist, D(c[3]));
            kcal = Math.Max(kcal, (int)D(c[4]));
            if (sp > 0) { sum += sp; n++; max = Math.Max(max, sp); }
        }
        return new SessionSummary(System.IO.Path.GetFileName(path), path, start, TimeSpan.FromSeconds(maxElapsed),
            Math.Round(dist / 1000, 2), n > 0 ? Math.Round(sum / n, 1) : 0, max, kcal);
    }

    async Task LoadSamplesAsync(SessionSummary? s)
    {
        Samples.Clear();
        if (s == null) return;
        var rows = await Task.Run(() => Rows(s.Path).Select(c => new SampleRow(
            DateTime.TryParse(c[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var t) ? t.ToLocalTime().ToString("HH:mm:ss") : c[0],
            c[1], c[2], c[3], c[4], c[5], c[6], c[7])).ToList());
        if (_selected != s) return;
        foreach (var r in rows) Samples.Add(r);
    }
}
