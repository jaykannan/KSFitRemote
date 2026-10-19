using System.Reflection;
using System.Text.Json;

namespace KsFit.Core;

public sealed record DeviceModel(
    string Id, string Brand, string Model, string Protocol, string Support,
    double? MinSpeed, double? MaxSpeed, string[] NameHints, string? ImageUrl, string Notes, string Source)
{
    public string Title => Brand == "KingSmith" ? Model : $"{Brand} {Model}";
}

public static class DeviceCatalog
{
    static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<DeviceModel> Load()
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("devices.json")!;
        return JsonSerializer.Deserialize<List<DeviceModel>>(s, Json)!;
    }

    /// <summary>True if an advertisement looks like a treadmill this app might drive.</summary>
    public static bool LooksLikeTreadmill(BleDeviceInfo d, IEnumerable<DeviceModel> catalog)
    {
        if (d.Services.Contains(FtmsTreadmill.Service) || d.Services.Contains(WalkingPadLegacy.Service)) return true;
        var n = d.Name.ToLowerInvariant();
        if (n.Length == 0) return false;
        return n.StartsWith("ks-") || n.Contains("walkingpad") || n.Contains("kingsmith")
            || catalog.Any(m => m.NameHints.Any(h => n.Contains(h.ToLowerInvariant())));
    }
}

public static class TreadmillFactory
{
    /// <summary>Picks the protocol from the services the connected device actually exposes.</summary>
    public static ITreadmill Create(IBleTransport ble)
    {
        if (ble.HasService(FtmsTreadmill.Service)) return new FtmsTreadmill(ble);
        if (ble.HasService(WalkingPadLegacy.Service)) return new WalkingPadLegacy(ble);
        throw new NotSupportedException(
            "This device exposes neither FTMS (0x1826) nor the WalkingPad service (0xFE00), so it isn't supported yet.");
    }
}
