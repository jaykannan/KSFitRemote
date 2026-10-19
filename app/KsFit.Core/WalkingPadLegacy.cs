namespace KsFit.Core;

/// <summary>
/// Older KingSmith WalkingPad protocol ("Type A": A1, A1 Pro, P1, R1 Pro, R2 ...): service 0xFE00,
/// notify 0xFE01, write 0xFE02, F7 A2 .. FD packets. Implemented from the public ph4-walkingpad project.
/// UNTESTED on real hardware by this project.
/// </summary>
public sealed class WalkingPadLegacy : ITreadmill
{
    public static readonly Guid Service = Guid.Parse("0000fe00-0000-1000-8000-00805f9b34fb");
    static readonly Guid Notify = Guid.Parse("0000fe01-0000-1000-8000-00805f9b34fb");
    static readonly Guid Write = Guid.Parse("0000fe02-0000-1000-8000-00805f9b34fb");
    static readonly TimeSpan MinSpacing = TimeSpan.FromMilliseconds(700);

    readonly IBleTransport _ble;
    readonly SemaphoreSlim _lock = new(1, 1);
    readonly CancellationTokenSource _cts = new();
    DateTime _last = DateTime.MinValue;
    MachineState _state = MachineState.Unknown;

    public string ProtocolName => "WalkingPad (legacy FE00, untested)";
    public double MinSpeed => 0.5;
    public double MaxSpeed => 6.0;
    public double SpeedStep => 0.1;
    public event Action<TreadmillData>? DataReceived;
    public event Action<MachineState>? StateChanged;

    public WalkingPadLegacy(IBleTransport ble) => _ble = ble;

    public async Task InitAsync()
    {
        await _ble.SubscribeAsync(Service, Notify, OnNotify);
        _ = Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                try { await Send(0x00, 0x00); await Task.Delay(1000, _cts.Token); }
                catch (OperationCanceledException) { }
                catch { await Task.Delay(1000); }
            }
        });
    }

    public async Task Start() { await Send(0x02, 0x01); await Send(0x04, 0x01); } // manual mode, then start
    public Task Pause() => Stop();                                              // no separate pause on this protocol
    public Task Stop() => Send(0x01, 0x00);
    public Task SetSpeed(double kmh) =>
        Send(0x01, (byte)Math.Round(Math.Clamp(kmh, MinSpeed, MaxSpeed) * 10));
    public Task CloseAsync() { _cts.Cancel(); return Task.CompletedTask; }

    async Task Send(byte cmd, byte arg)
    {
        await _lock.WaitAsync();
        try
        {
            var wait = MinSpacing - (DateTime.UtcNow - _last);
            if (wait > TimeSpan.Zero) await Task.Delay(wait);
            byte[] p = [0xF7, 0xA2, cmd, arg, 0, 0xFD];
            p[4] = (byte)((p[1] + p[2] + p[3]) & 0xFF);
            await _ble.WriteAsync(Service, Write, p);
            _last = DateTime.UtcNow;
        }
        finally { _lock.Release(); }
    }

    void OnNotify(byte[] d)
    {
        if (d.Length < 14 || d[0] != 0xF8 || d[1] != 0xA2) return;
        int beltState = d[2];
        double speed = d[3] / 10.0;
        int time = d[5] << 16 | d[6] << 8 | d[7];
        int dist = d[8] << 16 | d[9] << 8 | d[10];   // units of 10 m
        var st = beltState == 1 || speed > 0 ? MachineState.Running : MachineState.Stopped;
        if (st != _state) { _state = st; StateChanged?.Invoke(st); }
        DataReceived?.Invoke(new TreadmillData(speed, dist * 10.0, TimeSpan.FromSeconds(time), null, null, null));
    }
}
