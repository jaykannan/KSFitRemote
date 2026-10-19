namespace KsFit.Core;

public sealed record BleDeviceInfo(string Id, string Name, int Rssi)
{
    public override string ToString() => $"{Name}  ({Id})  {Rssi} dBm";
}

/// <summary>Minimal BLE surface the treadmill client needs. Implement once per platform.</summary>
public interface IBleTransport : IAsyncDisposable
{
    event Action? Disconnected;
    Task ScanAsync(Guid serviceFilter, TimeSpan duration, Action<BleDeviceInfo> onFound, CancellationToken ct);
    Task ConnectAsync(string deviceId, CancellationToken ct);
    Task DisconnectAsync();
    Task<byte[]> ReadAsync(Guid service, Guid characteristic);
    Task WriteAsync(Guid service, Guid characteristic, byte[] data);
    /// <summary>Enables notify (or indicate, if that's all the characteristic offers).</summary>
    Task SubscribeAsync(Guid service, Guid characteristic, Action<byte[]> onValue);
}
