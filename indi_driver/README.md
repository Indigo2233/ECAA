# INDI Driver for CAA Rotator

INDI driver for the CAA (Camera Angle Adjuster) — supports ESP8266 and Arduino
Nano hardware, over TCP or Serial.

Firmware repo: <https://github.com/Indigo2233/ECAA>

## Hardware Support

| Hardware | Transport | Default |
|----------|-----------|---------|
| ESP8266 | TCP (WiFi) | `192.168.4.1:4030` |
| ESP8266 | Serial (USB) | `/dev/ttyUSB0` @ 9600 |
| Arduino Nano | Serial (USB) | `/dev/ttyUSB0` @ 9600 |

All three use the same text protocol — switch transport in the INDI control
panel, no need to restart the driver.

## Build & Install

```bash
mkdir build && cd build
cmake .. -DCMAKE_INSTALL_PREFIX=/usr
make -j$(nproc)
sudo make install
```

## Run

```bash
# Standalone (waits for INDI client):
indi_caa_rotator

# With debug output:
INDI_DEBUG=1 indi_caa_rotator
```

In Ekos / KStars: Profile Editor → Rotator → **CAA Rotator**.

## INDI Properties

| Group | Property | Description |
|-------|----------|-------------|
| Transport | `TRANSPORT` | Switch between TCP / Serial |
| TCP Config | `TCP_HOST`, `TCP_PORT` | ESP8266 WiFi address |
| Serial Config | `SERIAL_PORT`, `SERIAL_BAUD` | Serial port and baud rate |
| Settings | `STEPS_PER_DEGREE` | Motor steps per degree (e.g. 635) |
| Settings | `CMD_TIMEOUT` | Command timeout in ms |
| Main | `GOTO` | Move to absolute sky angle |
| Main | `SYNC` | Align current position to sky angle |
| Main | `HOME` | Seek Hall sensor and return to zero |
| Main | `ABORT` | Halt movement |
| Info | `FW_VERSION` | Firmware version (read-only) |

## Protocol

Text commands terminated with `#`:

| Cmd | Description |
|-----|-------------|
| `G#` | Status: `P <steps>;M <moving>#` |
| `M <n>#` | Move to logical step position |
| `P <n>#` | Set current logical position (sync) |
| `H#` | Start homing |
| `S#` | Stop |
| `V#` | Firmware version |
| `D <n>#` | Set steps per degree (firmware int only) |

## Notes

- The driver sends `D <int>#` on connect to sync firmware steps-per-degree.
  The value is truncated to integer for firmware compatibility; full double
  precision is used for driver-side angle conversion.
- Serial transport uses POSIX termios (Linux/macOS) or Win32 CreateFile (Windows).
  Both serial and TCP work on all platforms.
- Homing is asynchronous; the driver polls `G#` until firmware reports idle.

## License

LGPL-2.1+ (same as libindi)
