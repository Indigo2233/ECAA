# ECAA ESP8266 Rotator

ECAA is an ESP8266-based electric camera angle adjuster / field rotator project.
It includes firmware for the controller, an ASCOM rotator driver for PC
astronomy software, a simple ASCOM test client, and mechanical reference assets.

## Features

- ESP8266 firmware with AP+STA WiFi.
- Mobile control page served by the ESP8266 at `http://192.168.4.1`.
- ASCOM-compatible TCP text protocol on port `4030`.
- WebSocket status updates on port `81`.
- Legacy Arduino Nano firmware kept as a protocol reference.
- ASCOM .NET Framework driver with Serial and TCP transport options.
- Mechanical assets for M42 and M54 variants.

## Repository Layout

- `ESP8266RotatorFirmware/`
  - Main ESP8266 firmware, firmware-specific README, and a browser-only mock
    control page.
- `ArduinoRotatorFirmware_ver1008/`
  - Original Arduino Nano firmware.
- `driver/scopefocusRotatorDriver/`
  - ASCOM rotator driver source. The folder name is historical; the public
    ASCOM driver identity is `ASCOM.ECAA.Rotator`.
- `driver/RotatorTest/`
  - Simple Windows Forms ASCOM test client.
- `m42/` and `m54/`
  - STL, SolidWorks, and reference image assets.
- `AGENTS.md`
  - Detailed implementation notes for future development agents.

## Default WiFi

- AP SSID: `CAA-Rotator-<chipid>`
- AP password: `012345678`
- Mobile control page: `http://192.168.4.1`
- ASCOM TCP port: `4030`
- WebSocket port: `81`

## Hardware Summary

Default ESP8266 board target:

- Wemos D1 mini or NodeMCU

Default wiring:

- STEP: D1 / GPIO5
- DIR: D2 / GPIO4
- ENABLE: D5 / GPIO14, active low
- HALL: D6 / GPIO12, active low
- CW: D7 / GPIO13, active low
- CCW: D0 / GPIO16, active low with an external 10k pull-up to 3.3V

Soldering guide for the photographed NodeMCU-style board:

- Orient the board with the USB connector at the bottom and the ESP8266 antenna
  at the top. Use the printed pin labels on the board as the reference.
- The required signal pads are on the right-side header in the photo:
  `D0`, `D1`, `D2`, `D5`, `D6`, `D7`, `GND`, and `3V3`.
- Solder pin headers first if the rotator will be serviced later. Direct wire
  soldering is acceptable for a permanent build; add strain relief near the
  board edge.

| Board pad | Connect to | Notes |
| --- | --- | --- |
| `D1` / GPIO5 | Stepper driver `STEP`, `PUL`, or `CLK` | 3.3V logic pulse output. |
| `D2` / GPIO4 | Stepper driver `DIR` | Direction output. Reverse in firmware or web UI if rotation is inverted. |
| `D5` / GPIO14 | Stepper driver `ENABLE` or `ENA` | Active low. The firmware pulls it low when the driver should be enabled. |
| `D6` / GPIO12 | Hall sensor output | Active low. Sensor output must stay at 3.3V or lower. |
| `D7` / GPIO13 | CW manual button | Wire the other side of the button to `GND`. |
| `D0` / GPIO16 | CCW manual button | Wire the other side of the button to `GND`; add a 10k pull-up from `D0` to `3V3`. |
| `3V3` | Hall sensor VCC, or driver logic VDD when supported | Use only for low-current 3.3V logic or sensors. |
| `GND` | Stepper driver logic GND, Hall GND, button common, and 12V supply negative | Common ground is required for STEP/DIR/ENABLE to be valid. |
| USB or `Vin` | ESP8266 board power | Prefer USB during testing. If using a buck converter, feed regulated 5V to `Vin` and `GND`. |

External stepper driver wiring:

| Driver terminal | Connect to | Notes |
| --- | --- | --- |
| `VMOT`, `12V+`, or motor supply `+` | External 12V positive | Set the driver current limit before load testing. |
| Motor supply `GND` | External 12V negative and ESP8266 `GND` | This is the shared power return. |
| `A+`, `A-`, `B+`, `B-` | Stepper motor coils | Swap one coil pair if the physical direction is unsuitable. |
| `VDD` or `VIO` | `3V3` only when the driver supports 3.3V logic | Use a level shifter or compatible driver for 5V-only logic inputs. |
| Microstep pins | Fixed high/low jumpers as required | Update `stepsPerDegree` after changing microstepping. |

Power requirements:

- Use an external 12V supply for the stepper driver motor power.
- Connect ESP8266 GND, stepper driver logic GND, and the 12V supply negative
  terminal together.
- Keep 12V away from ESP8266 `5V`, `3.3V`, and GPIO pins.
- ESP8266 GPIO uses 3.3V logic. Use level shifting or a 3.3V-compatible stepper
  driver when required.

Initial electrical check:

1. Power only the ESP8266 over USB and confirm the AP appears.
2. Wire `GND`, `STEP`, `DIR`, and `ENABLE` to the stepper driver with motor
   power disconnected.
3. Verify `ENABLE` changes state and `STEP` pulses during a small move command.
4. Connect the Hall sensor and confirm `Home` reacts when the sensor is
   triggered.
5. Connect the external 12V supply and motor after the driver current limit is
   set.

Home and zero behavior:

- The Hall sensor is only the mechanical reference point.
- `Set 0` stores the current physical position as the user-defined zero angle.
- The saved zero offset is persistent.
- `Home` finds the Hall sensor first, then automatically returns to the saved
  user-defined `0 deg` position.

## Firmware Build

Expected Arduino environment:

- Arduino ESP8266 core
- Board: Wemos D1 mini or NodeMCU
- Libraries: `AccelStepper`, `WebSocketsServer`

Open and upload:

```text
ESP8266RotatorFirmware/ESP8266RotatorFirmware.ino
```

Preview the mobile control page locally:

```text
ESP8266RotatorFirmware/mock-control-page.html
```

After upload:

1. Power the ESP8266.
2. Connect a phone to `CAA-Rotator-<chipid>` using password `012345678`.
3. Open `http://192.168.4.1`.
4. Verify status updates, halt, home, and small relative moves before attaching
   motor load.

## ASCOM Driver

Public identity:

- ProgID: `ASCOM.ECAA.Rotator`
- Chooser name: `ECAA ESP8266 Rotator`
- DLL name: `ASCOM.ECAA.Rotator.dll`

Build requirements:

- Visual Studio or MSBuild
- .NET Framework 4.8 Developer Pack
- ASCOM Platform developer components

Build command from repository root:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" driver\scopefocusRotatorDriver\scopefocusRotatorDriver.csproj /p:Configuration=Release /p:RegisterForComInterop=false /m
```

Driver setup defaults for ESP8266 TCP:

- Transport: `TCP`
- Host: `192.168.4.1`
- Port: `4030`
- Timeout: `3000`
- Steps/degree: `100`

## Legacy Text Protocol

Commands are ASCII text terminated by `#`.

- `G#`: returns `P <steps>;M <true|false>#`
- `M <steps>#`: move to absolute step position
- `P <steps>#`: set current logical step position and update the saved zero offset
- `H#`: start homing
- `S#`: stop movement
- `R <0|1>#`: set direction inversion
- `C <0|1>#`: set continuous hold
- `V#`: firmware version
- `I#`: JSON status
- `D <stepsPerDegree>#`: set steps per degree and recompute two-turn range

## Validation Checklist

- ESP8266 AP starts with the expected SSID and password.
- `http://192.168.4.1` loads the control page from a phone.
- `GET /api/status` returns JSON.
- TCP command `G#` on port `4030` returns a `#`-terminated response.
- ASCOM setup dialog shows `Serial` and `TCP` transport options.
- ASCOM test client can connect, read position, move, halt, and home.

## Development Notes

- Build outputs, Visual Studio caches, generated installers, DLLs, PDBs, and
  Arduino build artifacts are ignored by `.gitignore`.
- Keep `AGENTS.md` updated when driver identity, protocol, wiring, or build
  assumptions change.
