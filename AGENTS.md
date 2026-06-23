# ECAA Development Guide for Agents

This repository contains firmware, ASCOM driver code, test utilities, and mechanical assets for the ECAA ESP8266 electric camera angle adjuster.

## Project Map

- `ESP8266RotatorFirmware/`
  - ESP8266 Arduino sketch with AP+STA WiFi, mobile web UI, HTTP API, WebSocket status updates, TCP ASCOM-compatible text protocol, and serial debug protocol.
  - `README.md` contains wiring, default network, and legacy protocol notes.
- `ArduinoRotatorFirmware_ver1008/`
  - Original Arduino Nano firmware kept as a protocol and behavior reference.
- `driver/scopefocusRotatorDriver/`
  - ASCOM .NET Framework rotator driver source.
  - The project directory name is historical. The public ASCOM identity is ECAA-specific.
- `driver/RotatorTest/`
  - Simple Windows Forms test client using ASCOM DriverAccess.
- `m42/` and `m54/`
  - Mechanical STL, SolidWorks, and reference image assets.

## Fixed Public Identities

Keep these stable unless a new ASCOM driver slot is intentionally required:

- ASCOM ProgID: `ASCOM.ECAA.Rotator`
- ASCOM Chooser name: `ECAA ESP8266 Rotator`
- ASCOM driver description: `ASCOM Rotator Driver for ECAA ESP8266.`
- Driver DLL assembly name: `ASCOM.ECAA.Rotator.dll`
- ESP8266 AP SSID pattern: `CAA-Rotator-<chipid>`
- ESP8266 AP password: `012345678`
- Mobile control URL: `http://192.168.4.1`
- ASCOM TCP port: `4030`
- WebSocket port: `81`

## Firmware Notes

Primary file: `ESP8266RotatorFirmware/ESP8266RotatorFirmware.ino`

Expected Arduino environment:

- Board: Wemos D1 mini or NodeMCU
- Arduino ESP8266 core
- Libraries: `AccelStepper`, `WebSocketsServer`

Core behavior:

- Uses `AccelStepper::DRIVER` with STEP/DIR/ENABLE pins.
- Stores settings in EEPROM with `SETTINGS_MAGIC`.
- Default `stepsPerDegree` is `100`.
- Default travel range is two turns: `maxSteps = stepsPerDegree * 720`.
- Logical zero-degree angle is centered at `360 * stepsPerDegree`, matching the original ASCOM offset behavior.
- The mobile web page is embedded in `INDEX_HTML` and served from `/`.
- Status is available as JSON from `GET /api/status`.
- Movement and settings endpoints are implemented through `/api/move`, `/api/halt`, `/api/home`, `/api/set-position`, and `/api/settings`.

Legacy text commands terminate with `#`:

- `G#`: current position and moving state
- `M <steps>#`: move to absolute step position
- `P <steps>#`: set current step position
- `H#`: start homing
- `S#`: stop movement
- `R <0|1>#`: set direction inversion
- `C <0|1>#`: set continuous hold
- `V#`: firmware version
- `I#`: JSON status
- `D <stepsPerDegree>#`: set steps per degree and recompute the two-turn range

Hardware constraints:

- ESP8266 GPIO is 3.3V logic.
- Motor and stepper driver motor supply use external 12V power.
- ESP8266 GND, stepper driver logic GND, and 12V supply negative terminal must share ground.
- 12V must stay isolated from ESP8266 `5V`, `3.3V`, and GPIO pins.
- Default wiring is documented in `ESP8266RotatorFirmware/README.md`.

## ASCOM Driver Notes

Primary files:

- `driver/scopefocusRotatorDriver/Driver.cs`
- `driver/scopefocusRotatorDriver/RotatorConnection.cs`
- `driver/scopefocusRotatorDriver/SetupDialogForm.cs`
- `driver/scopefocusRotatorDriver/SetupDialogForm.designer.cs`

Architecture:

- `Driver.cs` implements ASCOM `IRotatorV2`.
- `IRotatorConnection` abstracts hardware communication.
- `SerialRotatorConnection` preserves the original ASCOM serial behavior.
- `TcpRotatorConnection` connects to ESP8266 TCP port `4030`.
- Setup UI stores `Transport`, `ComPort`, `TcpHost`, `TcpPort`, `CommandTimeoutMs`, `StepsPerDegree`, `ContHold`, `SetPos`, and `Pos` in the ASCOM Profile.
- On TCP connection, the driver sends `D <stepsPerDegree>#` so firmware and ASCOM angle conversion stay aligned.

Build environment:

- Project target framework is `.NET Framework v4.8`.
- ASCOM Platform developer components are required.
- Visual Studio or MSBuild with .NET Framework 4.8 Developer Pack is required for the project target.

Useful build commands from repo root:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" driver\scopefocusRotatorDriver\scopefocusRotatorDriver.csproj /p:Configuration=Release /p:RegisterForComInterop=false /m
```

This workstation currently lacks the `.NET Framework v4.8` reference assemblies. Source compatibility was verified with the installed `v4.7.2` reference assemblies using:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" driver\scopefocusRotatorDriver\scopefocusRotatorDriver.csproj /p:Configuration=Release /p:TargetFrameworkVersion=v4.7.2 /p:RegisterForComInterop=false /m
```

The project should remain on `v4.8` unless there is a deliberate compatibility decision.

## Repository Hygiene

- `.gitignore` excludes Visual Studio caches, build outputs, generated installers, DLLs, PDBs, Arduino build outputs, and local OS files.
- Source, firmware sketches, ASCOM project files, test project files, and mechanical assets are versioned.
- Generated `bin/`, `obj/`, `.vs/`, installer `.exe`, and local ASCOM logs are excluded.
- The Git remote is `https://github.com/Indigo2233/ECAA.git`.
- Current branch is `main`.

## Verification Checklist

After changing firmware:

- Compile the ESP8266 sketch with the Arduino ESP8266 core.
- Confirm AP starts as `CAA-Rotator-<chipid>`.
- Open `http://192.168.4.1` from a phone connected to the AP.
- Test `GET /api/status`.
- Test TCP command `G#` on port `4030`.
- Test stop behavior before connecting motor power.

After changing ASCOM driver:

- Build the driver project.
- Confirm output DLL name remains `ASCOM.ECAA.Rotator.dll`.
- Confirm ProgID remains `ASCOM.ECAA.Rotator`.
- Open the setup dialog and verify both `Serial` and `TCP` transport options.
- Test TCP transport with host `192.168.4.1`, port `4030`.
- Use `RotatorTest` or another ASCOM client to verify connect, position, move absolute, move relative, halt, and home.

## Common Risk Areas

- Step-to-angle conversion uses the two-turn center offset. Check `stepsPerDegree` changes in both firmware and ASCOM driver.
- Homing sets current position to `maxSteps / 2`, which represents the middle of the two-turn range.
- WebSocket status uses port `81`; phone browsers connected to the ESP8266 AP must be able to reach that port.
- TCP command responses must include the trailing `#`, since ASCOM reads until `#`.
- `D0` / GPIO16 has limited ESP8266 interrupt and pull-up behavior. The current CCW button design assumes an external 10k pull-up.
- `ENABLE_PIN` is active low. Review driver hardware before changing enable polarity.
