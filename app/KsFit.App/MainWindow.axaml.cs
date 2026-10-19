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
        Opened += async (_, _) => await _vm.StartupAsync();
    }

    bool _closing;

    // Disconnect from the pad cleanly, then actually close.
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (_closing || _vm == null) return;
        e.Cancel = true;
        _closing = true;
        try { await _vm.DisconnectAsync(); } catch { }
        Close();
    }

    void CloseClick(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    void TitleDrag(object? s, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
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
