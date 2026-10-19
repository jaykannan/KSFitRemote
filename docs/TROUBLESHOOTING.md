# Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| Scan finds nothing | Pad not powered or in sleep. Wake it with its button. Make sure Bluetooth is on and the KS Fit phone app is **closed** (and the phone's Bluetooth off if it keeps reconnecting). |
| Pad listed but Connect fails | Another device holds the connection. Close the phone app, wait ~10 s, retry. On Windows, remove any stale pairing in Settings → Bluetooth & devices. |
| "Treadmill did not respond." | Link is poor or the pad dropped it. Move closer, reconnect. |
| "Control not granted." | Reconnect; the app requests control on connect. |
| "Invalid parameter" when setting speed | Outside 1.0–6.0 km/h. |
| Start works but speed stays 1.0 | The target is applied only after the belt moves; if it didn't move within 15 s the step is skipped. Set speed manually. |
| Speed readout is 0 for a few seconds after Start | Normal: the pad counts down before moving. |
| Connection lost warning | Belt keeps its last state. Use the pad's button to stop. |
| `dotnet build` can't find the Windows SDK | Needs .NET 8+ SDK on Windows; the app targets `net8.0-windows10.0.19041.0`. |
| Python script: `AttributeError: module 'enum'…` | A file in the script's folder is named like a stdlib module (`enum.py`). Rename it. |
| Python `-I` flag can't import `bleak` | `-I` ignores user site-packages where `pip --user` installed it; run without `-I` for these scripts. |

Settings file (target speed, last device): `%LOCALAPPDATA%\KsFitRemote\settings.json`. Delete it to reset auto-connect.
