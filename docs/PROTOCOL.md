# KS Fit Z1 Bluetooth protocol

The Z1 follows the Bluetooth SIG **Fitness Machine Service (FTMS, 0x1826)**. All values are little-endian. UUIDs below are 16-bit short forms of `0000XXXX-0000-1000-8000-00805f9b34fb`.

## Device identification

| Item | Observed |
|---|---|
| Advertised name | none (the advertisement carries the service `0x1826` only) |
| Address | `82:20:00:0F:33:E4` (random static; yours will differ; it equals the serial in `2A25`) |
| Model (`2A24`) | `KS-Z1D` |
| Serial (`2A25`) | `8220000F33E4` |
| Hardware (`2A27`) | `MKS` |
| Firmware (`2A26`) | `J41_V301.08.13` |
| Software (`2A28`) | `V0.0.6` |

Because there is no name, find the pad by scanning for service `0x1826`.

## GATT layout

| Service | Characteristic | Properties | Use |
|---|---|---|---|
| `180A` Device Information | `2A23–2A29` | read | identity strings above |
| `1826` FTMS | `2ACC` Feature | read | `44 12 00 00 01 00 00 00` |
| | `2ACD` Treadmill Data | notify | live metrics (~1 Hz) |
| | `2AD3` Training Status | read, notify | `01 01` (idle) |
| | `2AD4` Supported Speed Range | read | `64 00 58 02 0A 00` |
| | `2AD5` Supported Inclination Range | read | all zero (no incline) |
| | `2AD7` Supported Heart Rate Range | read | `00 00 00` |
| | `2AD9` Control Point | write, indicate | commands and responses |
| | `2ADA` Machine Status | read, notify | state change events |
| `FFC0`, `FFF0` | `FFC1/FFF1` notify, `FFC2/FFF2` write-no-response | | vendor, unexplored |
| `24e2521c-f63b-48ed-85be-c5330a00fdf7` | same-UUID notify and write chars | | vendor, unexplored |
| `FF00` | `FF01` write | | vendor, unexplored |

### Feature (`2ACC`)
`0x00001244` = total distance, step count, expended energy, elapsed time. Targets `0x00000001` = **speed target setting only**.
Step count is flagged as supported but is not present in the Treadmill Data notifications we saw.

### Speed range (`2AD4`)
Min `0x0064` = 1.00 km/h, max `0x0258` = 6.00 km/h, step `0x000A` = 0.10 km/h.

## Control Point (`2AD9`)

You must enable **indications** on `2AD9` and write with response. Every request gets one response:

```
80 <request opcode> <result>
```

Result: `01` success, `02` not supported, `03` invalid parameter, `04` operation failed, `05` control not permitted.

| Command | Bytes | Notes |
|---|---|---|
| Request control | `00` | send once after connecting |
| Set target speed | `02 <uint16 speed in 0.01 km/h>` | `02 C8 00` = 2.00 km/h. Out of range → result `03` |
| Start / resume | `07` | belt counts down, then runs at 1.0 km/h |
| Stop | `08 01` | belt stops |
| Pause | `08 02` | belt stops; `07` resumes |

Example capture (send → response):

```
00          → 80 00 01
07          → 80 07 01
02 C8 00    → 80 02 01 C8 00
02 32 00    → 80 02 03 32 00    (0.5 km/h rejected)
08 02       → 80 08 01 02
08 01       → 80 08 01 01
```

Note: for opcodes with parameters the pad appends the request parameter to the response, so parse only the first three bytes.

## Machine Status (`2ADA`)

| Bytes | Meaning |
|---|---|
| `04` | started or resumed by user |
| `02 01` | stopped by user |
| `02 02` | paused by user |
| `03` | stopped by safety key (standard; not observed) |

## Treadmill Data (`2ACD`)

Standard FTMS layout: a 16-bit flags field, then fields in flag-bit order. Flag bit 0 is inverted: **0 means instantaneous speed is present**.

| Bit | Field | Size |
|---|---|---|
| 0 (clear) | Instantaneous speed, 0.01 km/h | u16 |
| 1 | Average speed | u16 |
| 2 | Total distance, metres | u24 |
| 3 | Inclination (0.1 %) + ramp angle | s16 + s16 |
| 4 | Elevation gain | u16 + u16 |
| 5 / 6 | Instantaneous / average pace | u8 each |
| 7 | Energy: total kcal, per hour, per minute | u16 + u16 + u8 (`FFFF` = n/a) |
| 8 | Heart rate | u8 |
| 9 | MET | u8 |
| 10 | Elapsed time, seconds | u16 |
| 11 | Remaining time | u16 |
| 12 | Force on belt + power | s16 + s16 |

The Z1 sends flags `0x2484` (bits 2, 7, 10 and 13) in 17-byte packets. Bit 13 is reserved in the spec and the 3 trailing bytes are unknown; the app ignores them. Idle sample:

```
84 24 | 00 00 | 00 00 00 | 00 00 00 00 00 | 00 00 | 00 00 00
flags   speed   distance    energy           elapsed  (unknown)
```

## Behaviour notes

- Start always begins at the 1.0 km/h minimum after a few seconds' countdown; set the target speed once the belt is moving.
- The pad has a single connection slot; the phone app must be closed.
- There is no auto-keepalive requirement observed in short tests.
