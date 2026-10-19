namespace KsFit.Core;

public sealed record BleDeviceInfo(string Id, string Name, int Rssi, IReadOnlyList<Guid> Services)
{
    public override string ToString() => $"{(Name.Length > 0 ? Name : "Treadmill")}  ({Id})  {Rssi} dBm";
}

/// <summary>Minimal BLE surface the treadmill client needs. Implement once per platform.</summary>
public interface IBleTransport : IAsyncDisposable
{
    event Action? Disconnected;
    /// <summary>Unfiltered scan; reports each device once at the end with merged name/services.</summary>
    Task ScanAsync(TimeSpan duration, Action<BleDeviceInfo> onFound, CancellationToken ct);
    bool HasService(Guid service);
    bool HasCharacteristic(Guid service, Guid characteristic);
    Task ConnectAsync(string deviceId, CancellationToken ct);
    Task DisconnectAsync();
    Task<byte[]> ReadAsync(Guid service, Guid characteristic);
    Task WriteAsync(Guid service, Guid characteristic, byte[] data);
    /// <summary>Enables notify (or indicate, if that's all the characteristic offers).</summary>
    Task SubscribeAsync(Guid service, Guid characteristic, Action<byte[]> onValue);
}
