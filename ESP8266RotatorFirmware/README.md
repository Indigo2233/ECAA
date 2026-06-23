# ESP8266RotatorFirmware

Arduino sketch for the ESP8266 CAA rotator controller.

`mock-control-page.html` is a browser-only preview of the control page. It uses
mock state and does not require an ESP8266.

## Board and libraries

- Board: Wemos D1 mini or NodeMCU
- Arduino ESP8266 core
- Libraries: `AccelStepper`, `WebSocketsServer`

## Default network

- AP SSID: `CAA-Rotator-<chipid>`
- AP password: `012345678`
- Control page: `http://192.168.4.1`
- ASCOM TCP protocol port: `4030`
- WebSocket status port: `81`

## Default wiring

- STEP: D1 / GPIO5
- DIR: D2 / GPIO4
- ENABLE: D5 / GPIO14, active low
- HALL: D6 / GPIO12, active low
- CW: D7 / GPIO13, active low
- CCW: D0 / GPIO16, active low with an external 10k pull-up to 3.3V

## Soldering guide

For the NodeMCU-style board shown in the project discussion, orient the board
with USB at the bottom and the ESP8266 antenna at the top. Follow the silkscreen
pin labels on the board.

| Board pad | Connect to | Notes |
| --- | --- | --- |
| `D1` / GPIO5 | Stepper driver `STEP`, `PUL`, or `CLK` | 3.3V logic pulse output. |
| `D2` / GPIO4 | Stepper driver `DIR` | Direction output. Toggle Reverse in the web UI if needed. |
| `D5` / GPIO14 | Stepper driver `ENABLE` or `ENA` | Active low. |
| `D6` / GPIO12 | Hall sensor output | Active low. Keep the output at 3.3V or lower. |
| `D7` / GPIO13 | CW manual button | Button shorts to `GND` when pressed. |
| `D0` / GPIO16 | CCW manual button | Button shorts to `GND`; add 10k from `D0` to `3V3`. |
| `3V3` | Hall VCC, or driver logic VDD when supported | Low-current 3.3V logic only. |
| `GND` | Driver logic GND, Hall GND, button common, and 12V negative | All logic signals require common ground. |
| USB or `Vin` | ESP8266 power | Use USB for testing, or regulated 5V to `Vin` and `GND`. |

External driver power:

- 12V positive goes to the stepper driver motor supply positive terminal.
- 12V negative goes to the stepper driver motor supply GND and ESP8266 `GND`.
- Stepper motor coils connect only to the driver motor outputs.
- Driver logic `VDD` / `VIO` may use ESP8266 `3V3` only when that driver accepts
  3.3V logic.
- Set the driver current limit before connecting the motor load.

Use an external 12V supply for the stepper driver motor power. Connect ESP8266
GND, stepper driver logic GND, and the 12V supply negative terminal together.
The ESP8266 GPIO pins are 3.3V logic. Do not connect 12V to the ESP8266 5V,
3.3V, or GPIO pins.

## Home and zero position

- The Hall sensor is used only as the mechanical reference point.
- `Set 0` stores the current physical position as the user-defined zero angle.
- The user-defined zero angle is saved in EEPROM as a step offset from the Hall
  reference.
- `Home` first finds the Hall sensor, resets the mechanical reference, then
  automatically moves to the saved user-defined `0 deg` position.

## Legacy protocol

Commands are ASCII text terminated by `#`.

- `G#`: returns `P <steps>;M <true|false>#`
- `M <steps>#`: move to an absolute step position
- `P <steps>#`: set the current logical step position and update the saved zero offset
- `H#`: start homing
- `S#`: stop movement
- `R <0|1>#`: set direction inversion
- `C <0|1>#`: set continuous hold
- `V#`: firmware version
- `I#`: JSON status
- `D <stepsPerDegree>#`: set steps per degree and recompute the two-turn range
