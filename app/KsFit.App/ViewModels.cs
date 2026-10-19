using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using Avalonia.Threading;
using KsFit.Core;

namespace KsFit.App;

public abstract class Observable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
    protected void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class AsyncCommand(Func<Task> run, Action<Exception> onError) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? p) => true;
    public async void Execute(object? p)
    {
        try { await run(); } catch (Exception e) { onError(e); }
    }
}

public sealed class MainViewModel : Observable
{
    readonly Func<IBleTransport> _newTransport;
    IBleTransport? _ble;
    ITreadmill? _tm;
    SessionLogger? _log;
    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KsFitRemote", "sessions");
    void LogEvent(string e) => _log?.Event(e, _state, _target);
    CancellationTokenSource? _speedDebounce;
    readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KsFitRemote", "settings.json");

    public ObservableCollection<BleDeviceInfo> Devices { get; } = new();

    // --- pad model selector ---
    readonly IReadOnlyList<DeviceModel> _catalog = DeviceCatalog.Load();
    readonly List<ModelItem> _allModels = new();
    public ObservableCollection<ModelItem> Models { get; } = new();
    ModelItem? _selectedModel;
    public ModelItem? SelectedModel
    {
        get => _selectedModel;
        set { if (value != null && Set(ref _selectedModel, value)) { Raise(nameof(PadTitle)); Raise(nameof(ModelNotice)); Raise(nameof(ModelHeader)); Save(); } }
    }
    public string PadTitle => _selectedModel is { Model: not null } m ? m.Title.ToUpperInvariant() : "WALKING PAD";
    public string ModelHeader => _selectedModel is { Model: not null } m ? $"Pad model: {m.Title}" : "Pad model: Auto-detect";
    public string ModelNotice => _selectedModel == null ? "" : _selectedModel.Notice;
    string _search = "";
    public string SearchText { get => _search; set { if (Set(ref _search, value)) FilterModels(); } }
    string? _savedModelId;

    void BuildModels()
    {
        _allModels.Add(new ModelItem(null));
        foreach (var m in _catalog) _allModels.Add(new ModelItem(m));
        _selectedModel = _allModels.FirstOrDefault(i => i.Id == _savedModelId) ?? _allModels[0];
        FilterModels();
    }

    void FilterModels()
    {
        var q = _search.Trim().ToLowerInvariant();
        var keep = _selectedModel;
        Models.Clear();
        foreach (var i in _allModels)
            if (q.Length == 0 || i == keep || i.Title.ToLowerInvariant().Contains(q) || i.Brand.ToLowerInvariant().Contains(q))
                Models.Add(i);
        _selectedModel = keep;
        Raise(nameof(SelectedModel));
    }

    BleDeviceInfo? _selected; public BleDeviceInfo? Selected { get => _selected; set { if (Set(ref _selected, value)) Raise(nameof(CanConnect)); } }
    bool _connected; public bool IsConnected { get => _connected; private set { Set(ref _connected, value); Raise(nameof(CanConnect)); Raise(nameof(IsDisconnected)); RaiseLifecycle(); } }
    public bool IsDisconnected => !_connected;
    bool _busy; public bool IsBusy { get => _busy; private set { Set(ref _busy, value); Raise(nameof(CanConnect)); } }
    public bool CanConnect => !_busy && !_connected && _selected != null;

    string _status = "Not connected"; public string Status { get => _status; private set => Set(ref _status, value); }
    string _error = ""; public string Error { get => _error; private set { Set(ref _error, value); Raise(nameof(HasError)); } }
    public bool HasError => _error.Length > 0;

    MachineState _state = MachineState.Unknown;
    public MachineState State
    {
        get => _state;
        private set { if (Set(ref _state, value)) RaiseLifecycle(); }
    }
    public bool IsRunning => _state == MachineState.Running;
    bool _starting;
    // Start sent, belt still counting down / not yet moving.
    public bool IsStarting => _starting || (_state == MachineState.Running && _currentSpeed <= 0);
    public string StateText => _connected switch
    {
        false => "OFFLINE",
        _ when IsStarting => "STARTING",
        _ => _state switch
        {
            MachineState.Running => "RUNNING",
            MachineState.Paused => "PAUSED",
            MachineState.Stopped => "STOPPED",
            _ => "READY"
        }
    };
    // Start: anything but an active run. Pause: only while running. Stop: always available when connected (safety).
    public bool CanStart => _connected && !_starting && _state != MachineState.Running;
    public bool CanPause => _connected && (_state == MachineState.Running || _starting);
    public bool CanStop => _connected;
    void RaiseLifecycle()
    {
        Raise(nameof(StateText)); Raise(nameof(IsRunning)); Raise(nameof(IsStarting));
        Raise(nameof(CanStart)); Raise(nameof(CanPause)); Raise(nameof(CanStop));
    }

    double _currentSpeed; public double CurrentSpeed { get => _currentSpeed; private set { if (Set(ref _currentSpeed, value)) { Raise(nameof(SpeedText)); Raise(nameof(IsStarting)); Raise(nameof(StateText)); } } }
    public string SpeedText => _currentSpeed.ToString("0.0");

    double _min = 1.0, _max = 6.0, _step = 0.1;
    public double MinSpeed { get => _min; private set => Set(ref _min, value); }
    public double MaxSpeed { get => _max; private set => Set(ref _max, value); }

    double _target = 2.0;
    public double TargetSpeed
    {
        get => _target;
        set
        {
            value = Math.Round(Math.Clamp(value, _min, _max) / _step) * _step;
            if (!Set(ref _target, Math.Round(value, 2))) return;
            Raise(nameof(TargetText));
            if (_state == MachineState.Running && !_starting) DebouncedApplySpeed();
            Save();
        }
    }
    public string TargetText => _target.ToString("0.0") + " km/h";

    string _distance = "0.00"; public string Distance { get => _distance; private set => Set(ref _distance, value); }
    string _time = "00:00"; public string Time { get => _time; private set => Set(ref _time, value); }
    string _kcal = "0"; public string Calories { get => _kcal; private set => Set(ref _kcal, value); }
    string _pace = "--"; public string Pace { get => _pace; private set => Set(ref _pace, value); }

    public ICommand ScanCmd { get; }
    public ICommand ConnectCmd { get; }
    public ICommand DisconnectCmd { get; }
    public ICommand StartCmd { get; }
    public ICommand PauseCmd { get; }
    public ICommand StopCmd { get; }
    public ICommand FasterCmd { get; }
    public ICommand SlowerCmd { get; }
    public ICommand PresetCmd { get; }
    public ICommand OpenLogsCmd { get; }
    public SessionBrowserViewModel LogBrowser { get; } = new(LogDirectory);

    public MainViewModel(Func<IBleTransport> newTransport)
    {
        _newTransport = newTransport;
        ScanCmd = new AsyncCommand(ScanAsync, OnError);
        ConnectCmd = new AsyncCommand(() => ConnectAsync(), OnError);
        DisconnectCmd = new AsyncCommand(DisconnectAsync, OnError);
        StartCmd = new AsyncCommand(() => { if (!CanStart) return Task.CompletedTask; LogEvent("cmd_start"); return StartAsync(); }, OnError);
        PauseCmd = new AsyncCommand(() => { if (!CanPause) return Task.CompletedTask; LogEvent("cmd_pause"); CancelStart(); return Guard(() => _tm!.Pause()); }, OnError);
        StopCmd = new AsyncCommand(() => { if (!CanStop) return Task.CompletedTask; LogEvent("cmd_stop"); CancelStart(); return Guard(() => _tm!.Stop()); }, OnError);
        OpenLogsCmd = new AsyncCommand(() =>
        {
            Directory.CreateDirectory(LogDirectory);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(LogDirectory) { UseShellExecute = true });
            return Task.CompletedTask;
        }, OnError);
        FasterCmd = new AsyncCommand(() => { TargetSpeed += 0.1; return Task.CompletedTask; }, OnError);
        SlowerCmd = new AsyncCommand(() => { TargetSpeed -= 0.1; return Task.CompletedTask; }, OnError);
        PresetCmd = new PresetCommand(v => TargetSpeed = v);
        Load();
        BuildModels();
    }

    void OnError(Exception e) => Dispatcher.UIThread.Post(() => { Error = e.Message; Status = IsConnected ? "Connected" : "Not connected"; });
    async Task Guard(Func<Task> a) { Error = ""; if (_tm == null) return; await a(); }

    public async Task ScanAsync()
    {
        if (IsBusy) return;
        IsBusy = true; Error = ""; Status = "Scanning…"; Devices.Clear();
        try
        {
            await using var ble = _newTransport();
            await ble.ScanAsync(TimeSpan.FromSeconds(6), d =>
            {
                if (!DeviceCatalog.LooksLikeTreadmill(d, _catalog)) return;
                Dispatcher.UIThread.Invoke(() => { Devices.Add(d); if (Selected == null || d.Id == _lastId) Selected = d; });
            }, CancellationToken.None);
            Status = Devices.Count == 0 ? "No treadmill found. Is it on and is the phone app closed?" : $"Found {Devices.Count}";
        }
        finally { IsBusy = false; }
    }

    bool _autoConnect = true;
    public bool AutoConnect { get => _autoConnect; set { if (Set(ref _autoConnect, value)) Save(); } }

    /// <summary>Scan; if auto-connect is on and the last pad is in range, connect to it.</summary>
    public async Task StartupAsync()
    {
        try
        {
            await ScanAsync();
            if (_autoConnect && _lastId != null && Devices.Any(d => d.Id == _lastId)) await ConnectAsync(_lastId);
        }
        catch (Exception e) { OnError(e); }
    }

    public async Task ConnectAsync(string? id = null)
    {
        if (IsBusy) return;
        id ??= Selected?.Id; if (id == null) return;
        IsBusy = true; Error = ""; Status = "Connecting…";
        try
        {
            _ble = _newTransport();
            _ble.Disconnected += () => Dispatcher.UIThread.Post(OnLinkLost);
            await _ble.ConnectAsync(id, CancellationToken.None);
            _tm = TreadmillFactory.Create(_ble);
            _tm.DataReceived += d => Dispatcher.UIThread.Post(() => OnData(d));
            _tm.StateChanged += s => Dispatcher.UIThread.Post(() => { State = s; LogEvent("state_" + s.ToString().ToLowerInvariant()); });
            await _tm.InitAsync();
            MinSpeed = _tm.MinSpeed; MaxSpeed = _tm.MaxSpeed; _step = _tm.SpeedStep;
            TargetSpeed = _target;
            IsConnected = true; State = MachineState.Unknown; Raise(nameof(StateText));
            Status = $"Connected · {_tm.ProtocolName}";
            _log = new SessionLogger(LogDirectory);
            LogEvent("connected");
            _lastId = id; Save();
        }
        catch { await CleanupAsync(); throw; }
        finally { IsBusy = false; }
    }

    CancellationTokenSource? _startCts;
    void CancelStart() { _startCts?.Cancel(); }

    async Task StartAsync()
    {
        await Guard(async () =>
        {
            var cts = _startCts = new CancellationTokenSource();
            _starting = true; RaiseLifecycle();
            try
            {
                await _tm!.Start();
                // The belt counts down and starts at minimum speed; apply the chosen speed once it moves.
                var until = DateTime.UtcNow.AddSeconds(15);
                while (DateTime.UtcNow < until && _currentSpeed <= 0 && !cts.IsCancellationRequested)
                    await Task.Delay(200, cts.Token);
                if (!cts.IsCancellationRequested && _tm != null && _currentSpeed > 0 && Math.Abs(_target - _currentSpeed) > 0.05)
                {
                    LogEvent($"set_speed_{_target:0.0}");
                    await _tm.SetSpeed(_target);
                }
            }
            catch (OperationCanceledException) { }
            finally { _starting = false; RaiseLifecycle(); }
        });
    }

    void DebouncedApplySpeed()
    {
        _speedDebounce?.Cancel();
        var cts = _speedDebounce = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, cts.Token);
                if (_tm != null) { LogEvent($"set_speed_{_target:0.0}"); await _tm.SetSpeed(_target); }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { OnError(e); }
        });
    }

    void OnData(TreadmillData d)
    {
        _log?.Sample(d, _state, _target);
        CurrentSpeed = d.SpeedKmh;
        if (d.DistanceM is { } m) Distance = (m / 1000).ToString("0.00");
        if (d.Elapsed is { } t) Time = t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"mm\:ss");
        if (d.Calories is { } k) Calories = k.ToString();
        Pace = d.SpeedKmh > 0 ? TimeSpan.FromMinutes(60 / d.SpeedKmh).ToString(@"m\:ss") + " /km" : "--";
        // Status events own the lifecycle; a decelerating belt after Pause/Stop must not flip us back to Running.
        // Only infer Running when we have no state yet (pad already moving when we connected).
        if (d.SpeedKmh > 0 && State == MachineState.Unknown) State = MachineState.Running;
    }

    void OnLinkLost()
    {
        if (!IsConnected) return;
        _ = CleanupAsync();
        Error = "Connection lost. The treadmill keeps its last state; use its own buttons to stop.";
    }

    public async Task DisconnectAsync() { await CleanupAsync(); Status = "Not connected"; }

    async Task CleanupAsync()
    {
        _speedDebounce?.Cancel(); CancelStart(); _starting = false;
        if (_tm != null) { try { await _tm.CloseAsync(); } catch { } }
        _tm = null;
        LogEvent("disconnected"); _log?.Dispose(); _log = null;
        if (LogBrowser.IsExpanded) _ = LogBrowser.RefreshAsync();
        if (_ble != null) { try { await _ble.DisposeAsync(); } catch { } _ble = null; }
        IsConnected = false; State = MachineState.Unknown; CurrentSpeed = 0;
        Raise(nameof(StateText));
    }

    // --- settings ---
    string? _lastId;
    public string? LastDeviceId => _lastId;
    void Load()
    {
        try
        {
            var s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_settingsPath));
            if (s != null) { _target = s.Target; _lastId = s.LastId; _autoConnect = s.AutoConnect; _savedModelId = s.ModelId; }
        }
        catch { }
    }
    void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(new Settings(_target, _lastId, _autoConnect, _selectedModel?.Id ?? _savedModelId)));
        }
        catch { }
    }
    sealed record Settings(double Target, string? LastId, bool AutoConnect = true, string? ModelId = null);
}

sealed class PresetCommand(Action<double> apply) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? p) => true;
    public void Execute(object? p) => apply(Convert.ToDouble(p, System.Globalization.CultureInfo.InvariantCulture));
}
