# Windows extensions for the 2026 IROS practice simulator

This branch builds on the Windows extension port and adds wheel-slip telemetry
and a live Slip HUD. The original extension-only branch remains `windows-extensions`.

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

## Per-wheel Slip HUD

Open the right HUD with the top-right button, then choose **Slip** beside
Ground Truth and Mapping. The expanded panel shows front left/front right above
rear left/rear right. Each wheel has a current dot, three seconds of history,
and raw lateral/longitudinal slip values.
Each value also shows `abs(raw slip) / configured peak` in parentheses, matching
the native-source calculation in the ROS stack's `docs/tire_lap_report.md`.
Plot coordinates use absolute ratios, matching the report's magnitude profiles.
The raw text and outgoing telemetry retain Unity's original signs. The offline
report takes the larger ratio on each axle; the HUD shows each wheel individually.

The horizontal axis is absolute lateral slip divided by `sidewaysFriction.extremumSlip`;
the vertical axis is absolute longitudinal slip divided by `forwardFriction.extremumSlip`.
Both axes start at 0 to 2 and automatically expand in powers of two to fit the
recent samples. All wheels share the same scales, shown below the charts. Scales
are held for five seconds before shrinking; dashed lines mark 1. These are peak-slip
coordinates from the configured friction curves, not a combined tire-force
boundary. No acceleration in g is inferred. Unity's slip signs are preserved.
Dots beyond the plotted range are red at the edge and labeled `off scale`.
An airborne wheel shows `no ground contact` and clears its trail.

The Slip HUD was checked in the graphical Windows simulator at rest and during
manual driving. A 120-sample slip/RPM/speed capture showed why a fixed range
clipped the longitudinal dots: at 3.318 m/s, one wheel reported 4079 RPM
(25.20 m/s of tire surface speed at radius 0.059 m) and forward slip 0.865.
That is consistent with substantial wheelspin and exceeds the old +/-0.30
longitudinal display range. The adaptive display retains the measured values
without clipping ordinary wheelspin/braking samples. Raw Unity signs are
preserved; the manual run showed positive slip with wheelspin.

An independent cross-check used `(wheel surface speed - vehicle speed) /
max(wheel surface speed, vehicle speed)` for positive wheel RPM, vehicle speeds
above 1 m/s, and absolute lateral slip below 0.002. Across 157 wheel samples,
its median absolute difference from native forward slip was 0.0041. This is a
diagnostic approximation and is not substituted for Unity telemetry. The old
kinematic lateral estimator is not used in the HUD; the report documents its
poor agreement with native lateral slip.

Readings are sampled on Unity's main thread at up to 20 Hz even without a bridge
connection. The Socket.IO `Bridge` frame also includes `V1 Wheel Slip`: four
`grounded forwardSlip sidewaysSlip` triples, ordered FL, FR, RL, RR. A wheel
with no ground hit is encoded as `0 0 0` (unavailable slip).

## Validation

The port compiles against the installed Windows 2026 IROS practice build.
Live checks have verified startup, control-port status, built-in track listing,
custom IROS 2026 import, switching to Berlin and back, obstacle commands, livery
reload, ground-truth map rendering, the Menu Map picker, screenshots, and
drag-to-place obstacles followed by removal. Mapping updates from ROS and
gamepad behavior still need a map sender and physical controller for validation.

The read-only control command `slip` returns raw slip, friction-curve peak coordinates, wheel RPM, and vehicle speed for diagnosing chart readings. Auto-ranging changes only the plotted scale; it does not smooth or alter the slip values.
