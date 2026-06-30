# Alt-Az De-Rotator Plugin for N.I.N.A.

Actively compensates for field rotation on Alt-Azimuth telescope mounts by
calculating the required rotation rate and commanding an ASCOM Rotator (e.g. ECAA).

Forked from [ArchitectJuan/Nina_Rotator_Plugin](https://github.com/ArchitectJuan/Nina_Rotator_Plugin)
with fixes for the ECAA/ASCOM ecosystem:

- **Accumulated error tracking**: slow field rotation rates (< 36°/hr) are
  accumulated across polling cycles and applied in one move when the deadband
  (0.01°) is reached, preventing stalls.
- **Direct MoveAbsolute**: bypasses the NINA mediator's `Move()` method which
  does not reliably call the ASCOM driver in NINA 3.x.
- **Diagnostic logging**: logs each derotation move with the applied angle
  and current field-rotation rate.

## Build

Requires .NET 8.0 SDK + local NINA installation.

```powershell
dotnet build AltAzDeRotator.csproj -c Release
```

## Install

1. Copy contents of `Builds\AltAz.DeRotator\` into
   `%localappdata%\NINA\Plugins\3.0.0\AltAz.DeRotator\`
2. Restart NINA.
3. Connect both Telescope (Alt-Az) and Rotator (e.g. ECAA) in Equipment tab.
4. Open **Alt-Az De-Rotator** dockable window, toggle **Enable De-Rotation** ON.

## License

MIT
- **Micro-step Prevention:** Intelligent 0.01-degree thresholding ensures your Rotator is only commanded when necessary, preventing unnecessary hardware wear.
- **N.I.N.A. 3.x Compatibility:** Built against the modern .NET 8.0 framework and N.I.N.A. 3.0 Managed Extensibility Framework (MEF).

## Version History
- **V1.1.0:** Added User Interface and On/Off switch control. Migrated to DockableVM pattern.
- **V1.0.0:** Initial Release. Features background polling service, dynamic rate calculation, and MEF dependency injection.

## Installation
Refer to the `UserGuide.md` for complete installation and usage instructions.

## Building from Source
This project requires Visual Studio or the .NET 8.0 SDK.
Run `dotnet build AltAzDeRotator.csproj` to compile the library. Output files are automatically copied to the `Builds\Nina_Rotator_Plugin_V1.0` folder.

<img width="1919" height="1045" alt="image" src="https://github.com/user-attachments/assets/01ff4a47-6c4b-45c0-91f5-5c990f143ca4" />
<img width="1919" height="1044" alt="image" src="https://github.com/user-attachments/assets/fb85dfe0-59e2-4403-958e-295583d68a73" />
<img width="455" height="245" alt="image" src="https://github.com/user-attachments/assets/394c7b31-d0e6-4c4b-8207-d081759b905a" />
