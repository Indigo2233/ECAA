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
  - Main ESP8266 firmware and firmware-specific README.
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

Power requirements:

- Use an external 12V supply for the stepper driver motor power.
- Connect ESP8266 GND, stepper driver logic GND, and the 12V supply negative
  terminal together.
- Keep 12V away from ESP8266 `5V`, `3.3V`, and GPIO pins.
- ESP8266 GPIO uses 3.3V logic. Use level shifting or a 3.3V-compatible stepper
  driver when required.

## Firmware Build

Expected Arduino environment:

- Arduino ESP8266 core
- Board: Wemos D1 mini or NodeMCU
- Libraries: `AccelStepper`, `WebSocketsServer`

Open and upload:

```text
ESP8266RotatorFirmware/ESP8266RotatorFirmware.ino
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
- `P <steps>#`: set current step position
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
