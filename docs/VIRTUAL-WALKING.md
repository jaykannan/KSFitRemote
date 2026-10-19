# Virtual walking and street view: options and how to hook them up

**Status: none of this is integrated yet.** KS Fit Remote controls the pad and logs sessions; it doesn't send your speed to anything else. This page lists the options found, which are free, and what has to be built to connect each one to the app. Items are tracked in [ROADMAP.md](ROADMAP.md).

## The one thing everything needs: a local speed feed

The pad allows **one Bluetooth connection at a time**. So an external app can't read the pad while KS Fit Remote is connected, and the other way round. The clean fix is for KS Fit Remote to stay the only thing connected and **publish speed to other programs on the same PC**:

- **Local API** on `127.0.0.1` only (HttpListener or Kestrel): a WebSocket stream plus `GET /state`, sending `{ "state": "running", "speedKmh": 2.5, "distanceM": 410, "elapsedS": 312 }` about once a second.
- Optional control endpoints (`/start`, `/stop`, `/speed`) behind a per-install token and **off by default**, because anything on the PC could otherwise move the belt.
- Small settings panel: enable, port, token.
- Reuses the existing data path (`ITreadmill.DataReceived` in the view model). Estimated work: small to medium.

Everything below except the VRChat/VRTI route assumes this feed exists. See the ready-made [`SessionLogger`](../app/KsFit.Core/SessionLogger.cs) for where the numbers come from.

## Options

| # | Option | Cost | Hook-up work | Effort |
|---|---|---|---|---|
| 1 | Walking-tour videos played at your speed | free | A small player page served by the app | small |
| 2 | Free virtual worlds via keyboard (W key) bridge | free | Windows key-injection bridge | small |
| 3 | VRChat via OSC | free (PC VRChat) | UDP OSC sender | small |
| 4 | VRChat via VRTI | VRTI paid, demo free | none in this app (but see conflict below) | none |
| 5 | Google Street View walker | free tier, needs card + key | Web page + Maps API | medium |
| 6 | Mapillary street-level walker | free token | Web page + Mapillary JS | medium |
| 7 | Phone apps (Remote Walk, Step Tracker, Treadmill TV) | free / in-app purchases | none possible, see notes | n/a |
| 8 | Pedestrian Simulator (open source, Fitbit based) | free, self-hosted | Fork to read our feed | medium |
| 9 | Zwift, Kinomap and similar | paid | Re-expose the pad as a Bluetooth treadmill | large, unverified |

### 1. Walking-tour videos (YouTube)
- **What:** first-person city walks on YouTube. Free. I know these exist but haven't verified specific channels.
- **Hook-up:** the app serves a page (`http://127.0.0.1:<port>/player`) embedding the video with YouTube's IFrame Player API and setting `playbackRate = yourSpeed / videoSpeed` from the speed feed. Walking videos are usually filmed at about 4-5 km/h. The player only allows certain rates (roughly 0.25x to 2x), so very slow or fast speeds clamp. Pause the video when the pad stops.
- **Needs:** local API; a page with a URL box and a "recorded speed" setting.

### 2. Free virtual worlds through a keyboard bridge
- **What:** any free site or game where walking is the **W** key (browser 3D worlds, VRChat in desktop mode, etc.).
- **Hook-up:** a Windows `SendInput` bridge in the app: hold **W** while speed is above a threshold, add Shift above a "run" speed. Optional per-app focus check so keys aren't sent to the wrong window.
- **Needs:** a bridge class and settings. **Caveat:** many online games forbid input automation, and anti-cheat can flag it. Use only in places that allow it.

### 3. VRChat through OSC
- **What:** VRChat is free on PC and accepts OSC input messages (such as `/input/Vertical`, `/input/Run`) over UDP, normally port 9000. From VRChat's OSC documentation; verify the addresses and port against the current docs when building it.
- **Hook-up:** send OSC floats to `127.0.0.1:9000` mapped from speed (e.g. `Vertical = clamp(speed / walkSpeed)`), `Run` above a run speed, and 0 when the pad is stopped or the link drops.
- **Needs:** a small OSC encoder over `UdpClient` (the format is simple), OSC enabled in VRChat's menu, and settings for port and mapping. No VRTI purchase needed.

### 4. VRChat via VRTI
- **What:** [VRTI](https://store.steampowered.com/app/4411910/VRTI__VR_Treadmill_Interface/?l=finnish) (paid, free demo) connects to the treadmill and drives VRChat and other games. Its [list](https://raphii.co/vrti/supported-devices/treadmills/) includes the KingSmith Z1D.
- **Hook-up:** nothing to build, but it talks to the pad itself, so **close KS Fit Remote while using it**. Use our app for control sessions and VRTI for gaming sessions.

### 5. Google Street View walker
- **What:** a page that moves you along a route in Street View as you walk.
- **Hook-up:** page uses the Maps JavaScript API (`StreetViewService`, panorama links), a route (Directions API or a hand-drawn path), and advances by `speed * dt` per tick, snapping to the next panorama. Needs a Google API key: see [GOOGLE-API-KEY.md](GOOGLE-API-KEY.md). Panoramas are spaced several metres apart, so movement is stepped, not smooth.
- **Needs:** local API; the page; key setting (git-ignored); a quota cap. **Check Google's terms** for what you may do with Street View data before publishing a tool.

### 6. Mapillary street-level imagery
- **What:** crowd-sourced street-level photos, free account and token, coverage varies by place.
- **Hook-up:** same idea as option 5 with Mapillary JS and its API instead of Google. No card needed.

### 7. Phone apps (Remote Walk, Virtual Walking: Step Tracker, Treadmill TV)
- **What:** [Remote Walk](https://apps.apple.com/us/app/remote-walk-virtual-walking/id1446750501) (3 free routes), [Virtual Walking: Step Tracker](https://apps.apple.com/us/app/-/id6446435705) (free), [Treadmill TV](https://apps.apple.com/app/id1418873208) (free with purchases). Listings seen were iOS.
- **Hook-up:** I found no way to feed external speed into them. Use them by matching your pace by hand. The same single-connection rule applies if any of them can connect to the pad itself.

### 8. Pedestrian Simulator
- **What:** open-source [Pedestrian Simulator](https://code.stevenpolley.net/steven/pedestrian-simulator/src/branch/main/README.md) that walks Google Street View from Fitbit steps.
- **Hook-up:** replace its Fitbit step source with our local feed, converting distance to steps (about 0.7 m per step, configurable). That means forking it and reading its code, which I haven't done. It needs a Google key too.

### 9. Zwift, Kinomap and similar
- **What:** paid platforms with rich worlds that usually connect to treadmills over Bluetooth FTMS. I haven't verified their treadmill support or free tiers.
- **Hook-up:** they would need to see a treadmill. Because only one program may connect to the pad, KS Fit Remote would have to **act as a Bluetooth FTMS treadmill itself** and relay to the real pad. Windows can publish BLE services but the details are untested here; treat this as a research item.

## Suggested order

1. Local speed feed (unlocks 1, 2, 5, 6, 8).
2. VRChat via OSC (3): free, small, and gives a real walkable world.
3. YouTube player page (1).
4. Street View or Mapillary walker (5 or 6).

## Safety

Anything that sends keyboard or OSC input must stop it when the pad stops, the link drops, or the app closes. Control endpoints must stay off by default and local-only.
