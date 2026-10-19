# Supported devices

> Unofficial project. Brand and model names are used only to describe compatibility; no affiliation with KingSmith, UREVO or any other vendor is implied.

The app picks the protocol from the services the pad actually exposes when you connect, so the **model selector is informational** (image, notes, speed-range hints, scan name hints). If a pad shows FTMS (`0x1826`) it is driven with FTMS; if it shows the legacy WalkingPad service (`0xFE00`) it is driven with the legacy protocol; otherwise connecting fails with a clear message.

## Support levels

| Level | Meaning |
|---|---|
| **Verified** | Run end-to-end on real hardware by this project |
| **Untested** | The protocol is implemented, but this project has never run it on that model. Reports from the community (VRTI) or the open-source project suggest it should work |
| **Not supported** | Known to use a proprietary protocol that is not documented/implemented here |
| **Unknown** | Newer model, protocol not confirmed; auto-detect is tried on connect |

Only the **KS Fit Z1 (KS-Z1D, FTMS)** is verified. The legacy WalkingPad driver ([`WalkingPadLegacy.cs`](../app/KsFit.Core/WalkingPadLegacy.cs)) was written from the MIT-licensed [ph4-walkingpad](https://github.com/ph4r05/ph4-walkingpad) project and has **never been run on a real pad**. Treat it with extra care, start at minimum speed, and please report results.

## Catalog

| Brand | Model | Protocol | Status |
|---|---|---|---|
| KingSmith | WalkingPad Z1 | FTMS | Verified |
| KingSmith | WalkingPad Z1F | FTMS | Untested |
| KingSmith | WalkingPad Z1E | KingSmith Type C | Not supported |
| KingSmith | WalkingPad Z1R | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad A1 Pro | WalkingPad legacy (Type A) | Untested |
| KingSmith | WalkingPad P1 | WalkingPad legacy (Type A) | Untested |
| KingSmith | WalkingPad R1 Pro | WalkingPad legacy (Type A) | Untested |
| KingSmith | WalkingPad R2 | WalkingPad legacy (Type A) | Untested |
| KingSmith | WalkingPad X21 | KingSmith Type B | Not supported |
| KingSmith | WalkingPad G15 | KingSmith Type C | Not supported |
| KingSmith | WalkingPad G1 | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad C2 | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad C2 Mini | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad MX3 | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad MX8 | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad MX10 | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad X214 | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad X218 | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad X25+ | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad Z3 Hybrid | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad Z3 Just Walk | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad R3 Hybrid | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad R3 Hybrid+ | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad G5i | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingPad W1 Plus Stride Station | auto-detect | Unknown (auto-detect) |
| KingSmith | WalkingCase | auto-detect | Unknown (auto-detect) |
| UREVO | SP1 Lite (URTM022) | FTMS | Untested |
| UREVO | SpaceWalk 3S | FTMS | Untested |
| UREVO | SpaceWalk E1L | FTMS | Untested |
| UREVO | Strol 2E (URTM030) | FTMS | Untested |
| UREVO | Strol 2S Pro | FTMS | Untested |
| KALWOL | X382P | FTMS | Untested |
| LifeSmart | H1 Hybrid | FTMS | Untested |
| XTERRA | TRX4500 | FTMS | Untested |
| Dynamax | RunningPad (DYCV-10221) | WalkingPad legacy (Type A) | Untested |
| Costway | SuperFit 76841235 | ESLinker | Not supported |

Source of the catalog: the KingSmith/WalkingPad and UREVO online stores (model names and photos), the [VRTI treadmill compatibility list](https://raphii.co/vrti/supported-devices/treadmills/) (protocol "types", community-reported), and the ph4-walkingpad project. Protocol type names (A/B/C) come from VRTI and are not official.

## Other compatible FTMS pads

Any treadmill implementing the standard Fitness Machine Service should work through **Auto-detect**, even if it is not in the list. The catalog also includes UREVO, KALWOL, LifeSmart and XTERRA models reported as generic FTMS. Incline and heart rate are not used by the app yet.

## Photos

Product photos belong to their manufacturers and are **not stored in this repository**. Run once after cloning:

```
pip install pillow
python tools/fetch_device_images.py
```

This downloads them into `app/KsFit.App/Assets/devices/` (git-ignored) from the URLs in [`devices.json`](../app/KsFit.Core/devices.json). Without them the selector shows a placeholder icon.

## Adding a model

Edit [`app/KsFit.Core/devices.json`](../app/KsFit.Core/devices.json) (embedded in the app). Fields: `id`, `brand`, `model`, `protocol` (`ftms`, `walkingpad-a`, `type-b`, `type-c`, `eslinker`, `auto`), `support` (`verified`, `untested`, `unsupported`, `unknown`), optional `minSpeed`/`maxSpeed`, `nameHints` (substrings of the advertised name), `imageUrl`, `notes`, `source`. Then run `tools/fetch_device_images.py`.

To get a model to **Verified**, run it and share what `tools/scan.py` and `tools/gatt_dump.py` print for it, plus the result of `tools/function_test.py` (change the address first).
