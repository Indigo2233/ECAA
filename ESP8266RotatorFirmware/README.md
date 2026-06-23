# ESP8266RotatorFirmware

Arduino sketch for the ESP8266 CAA rotator controller.

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

Use an external 12V supply for the stepper driver motor power. Connect ESP8266
GND, stepper driver logic GND, and the 12V supply negative terminal together.
The ESP8266 GPIO pins are 3.3V logic. Do not connect 12V to the ESP8266 5V,
3.3V, or GPIO pins.

## Legacy protocol

Commands are ASCII text terminated by `#`.

- `G#`: returns `P <steps>;M <true|false>#`
- `M <steps>#`: move to an absolute step position
- `P <steps>#`: set the current step position
- `H#`: start homing
- `S#`: stop movement
- `R <0|1>#`: set direction inversion
- `C <0|1>#`: set continuous hold
- `V#`: firmware version
- `I#`: JSON status
- `D <stepsPerDegree>#`: set steps per degree and recompute the two-turn range
