using Avalonia.Controls;
using Avalonia.Input;

namespace KsFit.App;

public partial class MainWindow : Window
{
    readonly MainViewModel _vm;

    public MainWindow() : this(null!) { }

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
        if (vm == null) return;
        Opened += async (_, _) =>
        {
            // Auto-reconnect to the last treadmill; fall back silently to manual scan.
            if (_vm.LastDeviceId is { } id)
                try { await _vm.ConnectAsync(id); } catch { await _vm.ScanAsync(); }
            else await _vm.ScanAsync();
        };
        Closing += (_, _) => _ = _vm.DisconnectAsync();
    }

    // Keyboard: Space = pause, Esc = stop, Up/Down = speed
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_vm == null || !_vm.IsConnected) return;
        switch (e.Key)
        {
            case Key.Escape: _vm.StopCmd.Execute(null); e.Handled = true; break;
            case Key.Space: (_vm.IsRunning ? _vm.PauseCmd : _vm.StartCmd).Execute(null); e.Handled = true; break;
            case Key.Up: _vm.FasterCmd.Execute(null); e.Handled = true; break;
            case Key.Down: _vm.SlowerCmd.Execute(null); e.Handled = true; break;
        }
    }
}
