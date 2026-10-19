# Architecture

```
app/
  KsFitRemote.sln
  KsFit.Core/                 net8.0, no platform code
    IBleTransport.cs          BLE abstraction (scan, connect, read, write, subscribe)
    FtmsTreadmill.cs          FTMS client: init, commands, parsing, state events
  KsFit.App/                  net8.0-windows10.0.19041.0, Avalonia 11
    Platform/WinRtBleTransport.cs   IBleTransport on Windows.Devices.Bluetooth
    ViewModels.cs             MainViewModel, small command/observable helpers
    MainWindow.axaml(.cs)     UI and keyboard shortcuts
    App.axaml(.cs), Program.cs
```

## Layers

1. **`IBleTransport`**: the only thing that touches a Bluetooth stack. Methods: `ScanAsync`, `ConnectAsync`, `DisconnectAsync`, `ReadAsync`, `WriteAsync`, `SubscribeAsync`, and a `Disconnected` event. `SubscribeAsync` picks notify, or indicate when notify is not offered (the control point only indicates).
2. **`FtmsTreadmill`**: protocol logic. `InitAsync` reads the speed range, subscribes to control-point, status and data characteristics, and requests control. `Start/Pause/Stop/SetSpeed` write to the control point and await the matching `80 <op> <result>` response (3 s timeout, one command in flight at a time via a semaphore). Non-success results throw. `TryParse` decodes Treadmill Data.
3. **`MainViewModel`**: UI state and commands. Device events arrive on background threads and are marshalled with `Dispatcher.UIThread`. It persists the last device id and target speed to `%LOCALAPPDATA%\KsFitRemote\settings.json`.
4. **Views**: Avalonia XAML, reflection bindings (`AvaloniaUseCompiledBindingsByDefault=false`), dark Fluent theme.

## Behaviours worth knowing

- **Start flow**: `Start` → wait up to 15 s for speed > 0 → `SetSpeed(target)` if it differs from the current speed.
- **Live speed change**: changing the target while running is debounced 300 ms, then sent.
- **State**: from Machine Status events (`04` running, `02 01` stopped, `02 02` paused). Non-zero speed in data also marks Running.
- **Connection loss**: the transport raises `Disconnected`; the VM tears down and shows a warning. The belt state is not changed.
- **Auto-connect**: on launch, if a last device id exists, it connects directly and falls back to a scan on failure.

## Porting to another platform

1. Split the XAML/ViewModel code into a shared `KsFit.UI` library (`net8.0`) and keep a thin head per platform (Windows, macOS/Linux desktop, Android).
2. Implement `IBleTransport` for the target:
   - macOS / iOS / Android: e.g. the `Plugin.BLE` or `InTheHand.BluetoothLE` packages, or native CoreBluetooth / Android BLE APIs.
   - Linux: BlueZ over D-Bus (e.g. `Tmds.DBus` or `Linux.Bluetooth`).
3. Pass the new transport factory to `MainViewModel` in that head's `App`.

`KsFit.Core` needs no changes. The same protocol is also easy to implement on an ESP32 (NimBLE) or in the browser (Web Bluetooth); see [PROTOCOL.md](PROTOCOL.md).

## Known limitations

- Windows BLE addresses are passed as MAC strings; the WinRT transport assumes the address type resolves automatically.
- No automated tests yet. `FtmsTreadmill.TryParse` is pure and the easiest place to add unit tests using the captured packets in PROTOCOL.md.
- No reconnect-on-drop; the user presses Connect again.
