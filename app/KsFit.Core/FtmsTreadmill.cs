using System.Buffers.Binary;

namespace KsFit.Core;

public enum MachineState { Unknown, Idle, Running, Paused, Stopped }

public sealed record TreadmillData(
    double SpeedKmh, double? DistanceM, TimeSpan? Elapsed, int? Calories, double? InclinePercent, int? HeartRate);

/// <summary>Standard Bluetooth FTMS treadmill client (service 0x1826).</summary>
public sealed class FtmsTreadmill
{
    public static readonly Guid Service = Uuid(0x1826);
    static readonly Guid DataChr = Uuid(0x2ACD), ControlChr = Uuid(0x2AD9),
        StatusChr = Uuid(0x2ADA), SpeedRangeChr = Uuid(0x2AD4);

    static Guid Uuid(int s) => Guid.Parse($"{s:x8}-0000-1000-8000-00805f9b34fb");

    readonly IBleTransport _ble;
    readonly SemaphoreSlim _cmdLock = new(1, 1);
    TaskCompletionSource<byte>? _pending;
    byte _pendingOp;

    public double MinSpeed { get; private set; } = 1.0;
    public double MaxSpeed { get; private set; } = 6.0;
    public double SpeedStep { get; private set; } = 0.1;

    public event Action<TreadmillData>? DataReceived;
    public event Action<MachineState>? StateChanged;

    public FtmsTreadmill(IBleTransport ble) => _ble = ble;

    public async Task InitAsync()
    {
        var r = await _ble.ReadAsync(Service, SpeedRangeChr);
        if (r.Length >= 6)
        {
            MinSpeed = BinaryPrimitives.ReadUInt16LittleEndian(r) / 100.0;
            MaxSpeed = BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(2)) / 100.0;
            SpeedStep = Math.Max(0.01, BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(4)) / 100.0);
        }
        await _ble.SubscribeAsync(Service, ControlChr, OnControlResponse);
        await _ble.SubscribeAsync(Service, StatusChr, OnStatus);
        await _ble.SubscribeAsync(Service, DataChr, d => { if (TryParse(d, out var t)) DataReceived?.Invoke(t); });
        await Command(0x00); // request control
    }

    public Task Start() => Command(0x07);
    public Task Pause() => Command(0x08, 2);
    public Task Stop() => Command(0x08, 1);

    public Task SetSpeed(double kmh)
    {
        kmh = Math.Round(Math.Clamp(kmh, MinSpeed, MaxSpeed), 2);
        var p = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(p, (ushort)Math.Round(kmh * 100));
        return Command(0x02, p);
    }

    async Task Command(byte op, params byte[] payload)
    {
        await _cmdLock.WaitAsync();
        try
        {
            _pendingOp = op;
            var pending = _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
            await _ble.WriteAsync(Service, ControlChr, [op, .. payload]);
            if (await Task.WhenAny(pending.Task, Task.Delay(3000)) != pending.Task)
                throw new TimeoutException("Treadmill did not respond.");
            var result = pending.Task.Result;
            if (result != 0x01)
                throw new InvalidOperationException(result switch
                {
                    0x02 => "Command not supported.",
                    0x03 => "Invalid parameter (speed out of range?).",
                    0x04 => "Operation failed.",
                    0x05 => "Control not granted.",
                    _ => $"Error {result:X2}"
                });
        }
        finally { _pending = null; _cmdLock.Release(); }
    }

    void OnControlResponse(byte[] d)
    {
        if (d.Length >= 3 && d[0] == 0x80 && d[1] == _pendingOp) _pending?.TrySetResult(d[2]);
    }

    void OnStatus(byte[] d)
    {
        if (d.Length == 0) return;
        StateChanged?.Invoke(d[0] switch
        {
            0x04 => MachineState.Running, // started/resumed by user
            0x02 => d.Length > 1 && d[1] == 2 ? MachineState.Paused : MachineState.Stopped,
            0x03 => MachineState.Stopped, // stopped by safety key
            _ => MachineState.Unknown
        });
    }

    public static bool TryParse(byte[] d, out TreadmillData t)
    {
        t = null!;
        if (d.Length < 4) return false;
        var flags = BinaryPrimitives.ReadUInt16LittleEndian(d);
        int i = 2;
        bool Has(int n) => i + n <= d.Length;
        double speed = 0; double? dist = null, incline = null; TimeSpan? elapsed = null; int? kcal = null, hr = null;

        if ((flags & 1) == 0) { speed = BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(i)) / 100.0; i += 2; }
        if ((flags & 1 << 1) != 0) i += 2;                                   // avg speed
        if ((flags & 1 << 2) != 0 && Has(3)) { dist = d[i] | d[i + 1] << 8 | d[i + 2] << 16; i += 3; }
        if ((flags & 1 << 3) != 0 && Has(4)) { incline = BinaryPrimitives.ReadInt16LittleEndian(d.AsSpan(i)) / 10.0; i += 4; }
        if ((flags & 1 << 4) != 0) i += 4;                                   // elevation gain
        if ((flags & 1 << 5) != 0) i += 1;                                   // inst pace
        if ((flags & 1 << 6) != 0) i += 1;                                   // avg pace
        if ((flags & 1 << 7) != 0 && Has(5))
        {
            var k = BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(i));
            if (k != 0xFFFF) kcal = k;
            i += 5;
        }
        if ((flags & 1 << 8) != 0 && Has(1)) { hr = d[i] == 0 ? null : d[i]; i += 1; }
        if ((flags & 1 << 9) != 0) i += 1;                                   // MET
        if ((flags & 1 << 10) != 0 && Has(2)) { elapsed = TimeSpan.FromSeconds(BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(i))); i += 2; }

        t = new TreadmillData(speed, dist, elapsed, kcal, incline, hr);
        return true;
    }
}
