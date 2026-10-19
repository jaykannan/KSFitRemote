using Avalonia.Media;
using Avalonia.Media.Imaging;
using KsFit.Core;

namespace KsFit.App;

public sealed class ModelItem
{
    public DeviceModel? Model { get; }
    public string Id => Model?.Id ?? "auto";
    public string Title { get; }
    public string Brand { get; }
    public string Badge { get; }
    public IBrush BadgeBrush { get; }
    public Bitmap? Image { get; }
    public bool HasImage => Image != null;
    public bool NoImage => Image == null;
    public string Notice { get; }

    public ModelItem(DeviceModel? m)
    {
        Model = m;
        if (m == null)
        {
            Title = "Auto-detect"; Brand = "Any pad"; Badge = "AUTO"; BadgeBrush = Brush.Parse("#3B6FD2");
            Notice = "Any pad that exposes FTMS (0x1826) or the legacy WalkingPad service (0xFE00) is detected on connect.";
            return;
        }
        Title = m.Title; Brand = m.Brand;
        (Badge, BadgeBrush) = m.Support switch
        {
            "verified" => ("VERIFIED", Brush.Parse("#1F9D55")),
            "untested" => ("UNTESTED", Brush.Parse("#C98A12")),
            "unsupported" => ("NOT SUPPORTED", Brush.Parse("#D23B3B")),
            _ => ("UNKNOWN", Brush.Parse("#6B7385"))
        };
        Notice = m.Notes;
        var f = Path.Combine(AppContext.BaseDirectory, "Assets", "devices", m.Id + ".png");
        try { if (File.Exists(f)) Image = new Bitmap(f); } catch { }
    }
}
