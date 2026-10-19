# KS Fit Remote

A replacement remote for the **KingSmith / WalkingPad "KS Fit Z1"** under-desk treadmill, for when the manufacturer's app or remote is lost or unsupported.

The Z1 exposes the standard Bluetooth **FTMS** (Fitness Machine Service) profile, so no proprietary protocol or cloud account is needed. This repo contains:

| Path | What it is |
|---|---|
| [`app/`](app) | Desktop app (C#, .NET 8, Avalonia UI). Start / pause / stop, live speed, target speed, stats. |
| [`ksz1.py`](ksz1.py) | Tiny keyboard remote in Python (`bleak`). Useful for quick tests. |
| [`tools/`](tools) | Scan, GATT dump, function-test and image-download scripts. |
| [`docs/`](docs) | [Protocol](docs/PROTOCOL.md), [Architecture](docs/ARCHITECTURE.md), [Reverse-engineering notes](docs/REVERSE-ENGINEERING.md), [Troubleshooting](docs/TROUBLESHOOTING.md), [Logging](docs/LOGGING.md), [Roadmap](docs/ROADMAP.md), [Supported devices](docs/DEVICES.md). |

> **Safety.** This sends real motor commands. Start at low speed, stay on or beside the belt, and keep the pad's own stop button reachable. If the Bluetooth link drops, the belt keeps its last state and the PC cannot stop it.

## Quick start (desktop app)

Requirements: Windows 10 19041+ with Bluetooth LE, [.NET 8 SDK](https://dotnet.microsoft.com/download) (or newer).

```
cd app
dotnet run --project KsFit.App
```

1. Power the treadmill on. **Close the KS Fit phone app** (the pad accepts one connection at a time).
2. The app scans automatically on first run. Pick the device and press **Connect**. With **Auto-connect to last pad if found** ticked (default), later launches connect automatically when that pad is in range.
3. Press **Start**. The belt counts down, then runs at 1.0 km/h (the pad's minimum). The app then applies your target speed.
4. Move the slider or use **+ / −** to change speed live.

Keyboard: `Space` start/pause, `Esc` stop, `↑` / `↓` speed. The ✕ in the title bar disconnects from the pad, then quits (a running belt keeps running).

Every connection is logged locally to a CSV (speed, distance, calories, commands) for future analytics; see [docs/LOGGING.md](docs/LOGGING.md).

Release build: `dotnet publish app/KsFit.App -c Release -r win-x64 --self-contained false`.

## Quick start (Python)

```
pip install bleak
python ksz1.py                          # scans for the first FTMS device
python ksz1.py --addr 82:20:00:0F:33:E4
```

Keys: `s` start, `x` stop, `+`/`-` speed ±0.5 km/h, `1`–`6` set speed, `q` quit. (`ksz1.py` uses `msvcrt`, so it is Windows-only.)

## Supported pads

The app has a **model selector** with a catalog of KS Fit/WalkingPad models and other pads reported to use standard FTMS (UREVO, KALWOL, LifeSmart, XTERRA). The protocol is chosen automatically from the services the pad exposes. **Only the KS Fit Z1 (KS-Z1D) is verified.** FTMS pads are *untested but expected to work*; the older WalkingPad protocol (A1, P1, R1 Pro, R2) is implemented from open-source docs and **has never been run on real hardware**; KingSmith "Type B/C" and ESLinker pads are not supported. See [docs/DEVICES.md](docs/DEVICES.md).

Model photos aren't stored in the repo; run `python tools/fetch_device_images.py` once (needs `pip install pillow`).

## What works (verified on the author's Z1, firmware `J41_V301.08.13`)

| Function | Verified |
|---|---|
| Connect, subscribe to data/status | yes |
| Request control | yes |
| Start | yes (belt starts at 1.0 km/h after a short countdown) |
| Set speed 1.5 / 2.0 km/h | yes |
| Out-of-range speed (0.5) | rejected by the pad as expected |
| Pause, resume, stop, start again | yes |
| Speeds above 2.0 km/h | **not tested** |
| Behavior on connection loss | **not tested** |
| The desktop app against hardware | yes (control, auto-connect and session logging) |

The Z1 reports a 1.0–6.0 km/h range in 0.1 steps. It supports speed target setting only (no incline). It does not report heart rate.

## Not covered

The KS Fit app's extras (auto mode, calibration, firmware update, workout programs, cloud sync) use other vendor services on the pad (`FFC0`, `FFF0`, `FF00`, `24e2521c-…`). They are documented only as far as discovery in [docs/REVERSE-ENGINEERING.md](docs/REVERSE-ENGINEERING.md) and are not implemented.

## Licence / disclaimer

Unofficial, not affiliated with KingSmith. Use at your own risk. Add a licence file of your choice before publishing.
