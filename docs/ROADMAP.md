# Roadmap / TODO

## Exercise programs
- [ ] Interval and ramp programs (e.g. "2.0 for 5 min, 3.5 for 1 min, repeat"), driven by the control point
- [ ] Program editor and saved presets; time- or distance-based targets
- [ ] Safe-guards: max speed cap, confirm before speeds above a threshold, auto-pause on connection loss
- [ ] Warm-up / cool-down templates

## Calories and analytics (uses the session logs, see [LOGGING.md](LOGGING.md))
- [ ] Session history view and summaries (time, distance, average speed)
- [ ] Better calorie estimate from user weight, speed and (optionally) incline, with a comparison against the pad's own value
- [ ] Charts of speed and calories over time; export to CSV/GPX/FIT
- [ ] Optional sync to Apple Health / Health Connect / Strava

## Virtual walking and online simulations
- [ ] Expose the pad as a standard FTMS peripheral/bridge so apps like Zwift or Kinomap can use it, or relay speed to them
- [ ] Local WebSocket/HTTP API so a browser or game can read speed and distance (and optionally set speed)
- [ ] Map/route playback that adjusts speed to a route's pace (Street View needs a Google API key: see [GOOGLE-API-KEY.md](GOOGLE-API-KEY.md))
- [ ] Investigate step count (advertised in the FTMS Feature field but not seen in data)

## Platform and app
- [ ] Split the UI into a shared library; macOS, Linux and Android heads with their own `IBleTransport`
- [ ] Reconnect-on-drop with backoff
- [ ] Unit tests for `FtmsTreadmill.TryParse` using the captured packets in [PROTOCOL.md](PROTOCOL.md)
- [ ] Verify speeds above 2.0 km/h and behaviour on connection loss (not yet tested)
- [ ] ESP32 hardware remote (physical buttons/knob) and a Web Bluetooth page

## More pads
- [ ] Verify the legacy WalkingPad driver (A1 Pro, P1, R1 Pro, R2) on real hardware
- [ ] Capture and document KingSmith Type B (X21, some R2) and Type C (Z1E, G15) protocols
- [ ] Confirm protocols for the newer models marked Unknown (Z3, R3, MX, X21x, G-series, W1)
- [ ] ESLinker support (Costway and others)
- [ ] Incline control for FTMS pads that support it (UREVO 3S/2S Pro, XTERRA)
- [ ] Read model/firmware from the Device Information service and pre-select the matching catalog entry

## Device features (needs capturing the KS Fit app's own traffic, see [REVERSE-ENGINEERING.md](REVERSE-ENGINEERING.md))
- [ ] Auto mode / standby, calibration, settings on the vendor services (`FFC0`, `FFF0`, `FF00`, `24e2521c-…`)
- [ ] Decode the 3 unknown trailing bytes in Treadmill Data
