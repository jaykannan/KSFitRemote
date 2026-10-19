using System.Globalization;
using KsFit.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace KsFit.App.Platform;

public sealed class WinRtBleTransport : IBleTransport
{
    BluetoothLEDevice? _dev;
    readonly Dictionary<(Guid, Guid), GattCharacteristic> _chars = new();
    public event Action? Disconnected;

    public async Task ScanAsync(Guid serviceFilter, TimeSpan duration, Action<BleDeviceInfo> onFound, CancellationToken ct)
    {
        var watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
        watcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(serviceFilter);
        var seen = new HashSet<ulong>();
        watcher.Received += (_, a) =>
        {
            lock (seen) if (!seen.Add(a.BluetoothAddress)) return;
            var mac = string.Join(":", BitConverter.GetBytes(a.BluetoothAddress).Take(6).Reverse().Select(b => b.ToString("X2")));
            var name = string.IsNullOrEmpty(a.Advertisement.LocalName) ? "KS Fit treadmill" : a.Advertisement.LocalName;
            onFound(new BleDeviceInfo(mac, name, a.RawSignalStrengthInDBm));
        };
        watcher.Start();
        try { await Task.Delay(duration, ct); } catch (OperationCanceledException) { }
        finally { watcher.Stop(); }
    }

    public async Task ConnectAsync(string deviceId, CancellationToken ct)
    {
        var addr = ulong.Parse(deviceId.Replace(":", ""), NumberStyles.HexNumber);
        _dev = await BluetoothLEDevice.FromBluetoothAddressAsync(addr) ?? throw new IOException("Device not found.");
        _dev.ConnectionStatusChanged += (d, _) =>
        {
            if (d.ConnectionStatus == BluetoothConnectionStatus.Disconnected) Disconnected?.Invoke();
        };
        var svcs = await _dev.GetGattServicesAsync(BluetoothCacheMode.Uncached);
        if (svcs.Status != GattCommunicationStatus.Success) throw new IOException($"GATT discovery failed: {svcs.Status}");
        foreach (var s in svcs.Services)
        {
            var cs = await s.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            if (cs.Status != GattCommunicationStatus.Success) continue;
            foreach (var c in cs.Characteristics) _chars[(s.Uuid, c.Uuid)] = c;
        }
    }

    public Task DisconnectAsync()
    {
        foreach (var c in _chars.Values) c.Service.Dispose();
        _chars.Clear();
        _dev?.Dispose();
        _dev = null;
        return Task.CompletedTask;
    }

    GattCharacteristic Get(Guid s, Guid c) =>
        _chars.TryGetValue((s, c), out var ch) ? ch : throw new IOException($"Characteristic {c} missing.");

    public async Task<byte[]> ReadAsync(Guid service, Guid characteristic)
    {
        var r = await Get(service, characteristic).ReadValueAsync(BluetoothCacheMode.Uncached);
        if (r.Status != GattCommunicationStatus.Success) throw new IOException($"Read failed: {r.Status}");
        return ToBytes(r.Value);
    }

    public async Task WriteAsync(Guid service, Guid characteristic, byte[] data)
    {
        var r = await Get(service, characteristic).WriteValueWithResultAsync(ToBuffer(data), GattWriteOption.WriteWithResponse);
        if (r.Status != GattCommunicationStatus.Success) throw new IOException($"Write failed: {r.Status}");
    }

    public async Task SubscribeAsync(Guid service, Guid characteristic, Action<byte[]> onValue)
    {
        var ch = Get(service, characteristic);
        ch.ValueChanged += (_, e) => onValue(ToBytes(e.CharacteristicValue));
        var mode = ch.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify)
            ? GattClientCharacteristicConfigurationDescriptorValue.Notify
            : GattClientCharacteristicConfigurationDescriptorValue.Indicate;
        var st = await ch.WriteClientCharacteristicConfigurationDescriptorAsync(mode);
        if (st != GattCommunicationStatus.Success) throw new IOException($"Subscribe failed: {st}");
    }

    static byte[] ToBytes(IBuffer b) { var a = new byte[b.Length]; DataReader.FromBuffer(b).ReadBytes(a); return a; }
    static IBuffer ToBuffer(byte[] d) { var w = new DataWriter(); w.WriteBytes(d); return w.DetachBuffer(); }

    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
