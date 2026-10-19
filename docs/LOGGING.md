# Session logging

Every connection writes one CSV file to:

```
%LOCALAPPDATA%\KsFitRemote\sessions\session-YYYYMMDD-HHMMSS.csv
```

The **Open session logs** button in the app opens this folder. Logging is local only; nothing is sent anywhere.

## Format

`timestamp_utc,elapsed_s,speed_kmh,distance_m,kcal,state,target_kmh,event`

| Column | Meaning |
|---|---|
| `timestamp_utc` | ISO-8601 UTC time of the row |
| `elapsed_s` | Treadmill's own elapsed-time counter (blank on event rows) |
| `speed_kmh` | Measured belt speed (blank on event rows) |
| `distance_m` | Treadmill's cumulative distance (blank on event rows) |
| `kcal` | Treadmill's cumulative calorie estimate (blank on event rows) |
| `state` | App's view of the machine: `Unknown`, `Running`, `Paused`, `Stopped` |
| `target_kmh` | The target speed selected in the app |
| `event` | Empty on sample rows (about 1 per second). On event rows: `connected`, `disconnected`, `cmd_start`, `cmd_pause`, `cmd_stop`, `set_speed_<kmh>`, `state_running`, `state_paused`, `state_stopped` |

Sample rows have an empty `event`; event rows have empty measurement columns, so the two are easy to filter.

Notes: the treadmill's counters reset when a new run starts, and `state` is `Unknown` until the first status event. Calories are the pad's own estimate, not derived from body weight.

Implementation: [`SessionLogger`](../app/KsFit.Core/SessionLogger.cs), hooked in `MainViewModel`.
