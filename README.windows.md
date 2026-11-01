# Windows extensions for the 2026 IROS practice simulator

This branch builds on `windows-il2cpp-compat` and ports the existing macOS
extension modules to BepInEx 6 on Windows IL2CPP. Wheel-slip telemetry remains
on its separate branch.

Features include built-in and custom track selection, switching and reset/lap
state updates; autoconnect and safe disconnect; the TCP control API and Menu
Map button; livery reload; Ground Truth and Mapping tabs with map/localization
error; draggable obstacles; and manual gamepad driving with power levels and
steering/throttle assists. Windows uses XInput instead of macOS libusb.

## Install

1. Copy the stock Windows practice simulator folder to a new directory.
2. Extract BepInEx **Unity IL2CPP win-x64** into that copy, beside
   `AutoDRIVE Simulator.exe`. Use the official
   [build downloads](https://builds.bepinex.dev/projects/bepinex_be).
   The tested loader is `6.0.0-be.791+b782da4`.
3. Start the copied simulator once and close it. BepInEx generates
   `BepInEx/interop/Assembly-CSharp.dll` and the Unity bindings.
4. Install a .NET 8 SDK. From this repository in PowerShell, run:

```powershell
./install_windows.ps1 -SimulatorDir 'D:\Downloads\autodrive_windows_extensions'
```

The script builds the plugin against that simulator's bindings and installs
the DLL, custom tracks, and livery. Close the simulator before installing.

Launch the copied simulator:

```powershell
& 'D:\Downloads\autodrive_windows_extensions\AutoDRIVE Simulator.exe' -track iros2026 -autoconnect
```

Use `-control-port 4580` and `-mapview-port 4581` to choose the API and Mapping
receiver ports. The bridge uses the simulator's own `-ip` and `-port` arguments.
All existing command names and map-view wire messages remain supported:

```powershell
./windows_ctl.ps1 -Command status
./windows_ctl.ps1 -Command tracks
./windows_ctl.ps1 -Command 'track berlin'
./windows_ctl.ps1 -Command 'obstacle 1 1'
./windows_ctl.ps1 -Command 'obstacle clear'
./windows_ctl.ps1 -Command livery
```

See the main README for the full control protocol. Configure the ROS map-view
sender to reach the Windows host on port 4581. For XInput pads, use X mode on
Logitech F710/F310 controllers. The first connected pad is selected, and manual
driving retains the original extension's power, brake/reverse, and assist logic.

## Port details

Windows copies of the modules live in `track_select_windows/Extensions/`.
They adapt injected MonoBehaviour constructors and scene lifecycle, Unity
callbacks and UI events, native collection/cast handling, JSON field loading,
and generated IL2CPP properties for the stock simulator's private state.
The original macOS sources remain in `track_select/`.

The plugin targets .NET 6 for the loader. No stock game assemblies or generated
interop binaries are committed to this repository.

## Validation

The port compiles against the installed Windows 2026 IROS practice build.
Live checks have verified startup, control-port status, built-in track listing,
custom IROS 2026 import, switching to Berlin and back, obstacle commands, livery
reload, ground-truth map rendering, the Menu Map picker, screenshots, and
drag-to-place obstacles followed by removal. Mapping updates from ROS and
gamepad behavior still need a map sender and physical controller for validation.
