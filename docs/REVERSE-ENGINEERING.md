# Reverse-engineering notes

How the protocol was found, and where to dig further.

## The app

`KS+Fit_6.6.0_APKPure.xapk` is a zip of split APKs (`com.kingsmith.xiaojin`, version 6.6.0, code 5304). Unzip it, then unzip `com.kingsmith.xiaojin.apk` (base) and `config.arm64_v8a.apk` (native libs).

- It is a **Flutter** app. Application logic is Dart AOT in `lib/arm64-v8a/libapp.so`; the dex files only contain plugins and SDKs, so Java decompilers (jadx) are not useful.
- BLE uses `flutter_blue_plus` underneath an in-house package `ks_blue`. Dart source paths remain in the binary's strings, e.g. `ks_blue/src/wilink/wilink_protocol.dart`, `wilink_device.dart`, `treadmill_data.dart`, `ftms_action_executor.dart`, `ble/protocol.dart`, `dual/…`, `fitshow/…`, `ota/…`.
- The device-model list (`ksmb.treadmill.k12pro`, `ksmb.walkingpad.s1`, `KS-…` scan names) and service UUIDs (`1826`, `FE00/FE01/FE02`, `FFF1`, `FEE7`, `FDF0`, `24e2521c-…`) are visible with a simple strings search.

Useful technique (no tools needed beyond Python): extract printable strings from `libapp.so` and grep for UUIDs and `package:ks_blue`.

To recover real function names and offsets, use [Blutter](https://github.com/worawit/blutter) or reFlutter on `libapp.so` with the matching Flutter engine version (bundled `libflutter.so`).

## What the Z1 actually speaks

Scanning the real device (`tools/scan.py`) showed service `0x1826` in the advertisement, and a GATT dump (`tools/gatt_dump.py`) showed a complete FTMS service. The commands were then verified directly with `tools/function_test.py`. Full details in [PROTOCOL.md](PROTOCOL.md). No reverse engineering of the app was required for control.

## Unexplored

| Item | Hint |
|---|---|
| Vendor services `FFC0`, `FFF0`, `FF00`, `24e2521c-…a00fdf7` | probably the "WiLink" supplement protocol (`wrapSupplementCmd`, `ftms_supplement.dart`) used for auto mode, calibration, OTA, settings |
| Trailing 3 bytes in Treadmill Data | unknown (flag bit 13 is reserved in the spec) |
| Step count | advertised in Feature but not seen in data |
| Other firmware versions | only `J41_V301.08.13` tested |

### Capturing the app's own traffic (best way to decode the vendor services)

1. On Android: Developer options → **Enable Bluetooth HCI snoop log**.
2. Toggle Bluetooth, use the KS Fit app (auto mode, calibration, etc.), then pull `btsnoop_hci.log` (via `adb bugreport` on newer Android).
3. Open it in Wireshark, filter `btatt`, and match handles to the characteristics from `tools/gatt_dump.py`.

## Tools in this repo

| Script | Purpose |
|---|---|
| `tools/scan.py` | Lists BLE devices with RSSI, service UUIDs and manufacturer data |
| `tools/gatt_dump.py <addr>` | Connects, lists services/characteristics, reads values, subscribes for 6 s. Read-only |
| `tools/function_test.py` | Runs the command sequence (hard-coded address `82:20:00:0F:33:E4`) and prints responses. **Moves the belt** (speeds ≤ 2.0 km/h) |

All use `bleak` (`pip install bleak`). Do not name a script after a stdlib module (e.g. `enum.py`); it breaks Python's imports.

## Legal note

Interoperability with hardware you own. The extracted APK and its contents belong to KingSmith and are not redistributed here.
