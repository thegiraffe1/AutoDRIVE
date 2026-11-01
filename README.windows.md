# Windows Extensions for the 2026 IROS Practice Simulator

This branch builds on the Windows extension port and adds wheel-slip telemetry
and a live Slip HUD. The original extension-only branch remains `windows-extensions`.

Features include built-in and custom track selection, switching and reset/lap
state updates; autoconnect and safe disconnect; the TCP control API and Menu
Map button; livery reload; Ground Truth and Mapping tabs with map/localization
error; draggable obstacles; and manual gamepad driving with power levels and
steering/throttle assists. Windows uses XInput instead of macOS libusb.

---

## Quick Install (step-by-step)

### Prerequisites

| Tool | Why you need it |
|------|----------------|
| **AutoDRIVE Practice Simulator** (Windows) | The base simulator `.zip` from the IROS 2026 competition |
| **Git** | To clone this repository |
| **.NET 8 SDK** | To build the BepInEx plugin DLL |

### Step 1 — Copy the simulator

Extract (or copy) the stock Windows practice simulator folder somewhere convenient.
The folder should contain `AutoDRIVE Simulator.exe`, `GameAssembly.dll`, etc.

```
📁 autodrive_simulator/
├── AutoDRIVE Simulator.exe
├── GameAssembly.dll
├── UnityPlayer.dll
├── Data/
└── ...
```

### Step 2 — Install BepInEx 6 (Unity IL2CPP build)

> [!IMPORTANT]
> You need the **Unity IL2CPP** variant, **not** the Unity Mono variant.
> The simulator uses IL2CPP. Using the wrong one will silently fail.

1. Go to [BepInEx Bleeding Edge builds](https://builds.bepinex.dev/projects/bepinex_be).
2. Under build **#791**, download:
   **`BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.791+b782da4.zip`**

   Direct link: [Download BepInEx IL2CPP #791](https://builds.bepinex.dev/projects/bepinex_be/791/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.791%2Bb782da4.zip)

3. Extract the zip **directly into your simulator folder** (beside `AutoDRIVE Simulator.exe`).
   After extraction you should see:

```
📁 autodrive_simulator/
├── AutoDRIVE Simulator.exe
├── BepInEx/              ← new
│   ├── core/
│   ├── config/
│   ├── plugins/
│   └── ...
├── dotnet/               ← new (bundled CoreCLR for IL2CPP)
├── doorstop_config.ini   ← new/replaced
├── winhttp.dll           ← new/replaced
└── ...
```

### Step 3 — Generate interop assemblies (first launch)

Run the simulator once and **wait for it to fully load** (~60–80 seconds).
On the first BepInEx-enabled launch, it downloads Unity base libraries and
generates IL2CPP interop bindings in `BepInEx/interop/`.

```powershell
& '.\AutoDRIVE Simulator.exe'
```

Wait until the simulator window appears and the game is visibly running, then
close it. Verify interop was generated:

```powershell
Test-Path '.\BepInEx\interop\Assembly-CSharp.dll'
# Should print: True
```

> [!WARNING]
> If `Assembly-CSharp.dll` does not appear, check `BepInEx/ErrorLog.log` and
> ensure you used the **IL2CPP** BepInEx build (not Mono). The `doorstop_config.ini`
> should show `target_assembly = BepInEx\core\BepInEx.Unity.IL2CPP.dll`.

### Step 4 — Install .NET 8 SDK

If you don't already have it, install a .NET 8 SDK. The easiest way without
needing admin is Microsoft's install script:

```powershell
# Download and run the official install script (no admin required)
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile "$env:TEMP\dotnet-install.ps1"
& "$env:TEMP\dotnet-install.ps1" -Channel 8.0
```

This installs the SDK to `%LOCALAPPDATA%\Microsoft\dotnet`. Add it to your
current session:

```powershell
$env:DOTNET_ROOT = "$env:LOCALAPPDATA\Microsoft\dotnet"
$env:Path = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:Path"

# Verify
dotnet --list-sdks
# Should show something like: 8.0.427 [...]
```

> [!TIP]
> To make this permanent, add `%LOCALAPPDATA%\Microsoft\dotnet` to your user
> PATH via System Settings → Environment Variables, or run:
> ```powershell
> $p = [Environment]::GetEnvironmentVariable("Path","User")
> [Environment]::SetEnvironmentVariable("Path","$p;$env:LOCALAPPDATA\Microsoft\dotnet","User")
> [Environment]::SetEnvironmentVariable("DOTNET_ROOT","$env:LOCALAPPDATA\Microsoft\dotnet","User")
> ```

### Step 5 — Add NuGet source (if needed)

A fresh `dotnet-install.ps1` SDK may not have any NuGet sources configured.
Check and add `nuget.org` if missing:

```powershell
dotnet nuget list source
# If it says "No sources found.", add it:
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

### Step 6 — Clone this repo and install the plugin

```powershell
git clone --depth 1 -b windows-wheel-slip-hud https://github.com/thegiraffe1/AutoDRIVE.git
cd AutoDRIVE
```

Run the install script, pointing it at your simulator copy. **Close the simulator first.**

```powershell
./install_windows.ps1 -SimulatorDir 'C:\path\to\autodrive_simulator'
```

This builds the plugin DLL and copies it plus custom tracks and livery into the
simulator. You should see:

```
Build succeeded.
Installed Windows extensions into C:\path\to\autodrive_simulator
```

### Step 7 — Launch

```powershell
& 'C:\path\to\autodrive_simulator\AutoDRIVE Simulator.exe' -track iros2026 -autoconnect
```

The first plugin-enabled launch takes **~80 seconds** to fully initialize.
Once loaded, the control port opens on `127.0.0.1:4580`.

---

## Control API

Use `windows_ctl.ps1` (in this repo) to send commands to the running simulator:

```powershell
./windows_ctl.ps1 -Command status          # JSON status
./windows_ctl.ps1 -Command tracks           # List available tracks
./windows_ctl.ps1 -Command 'track berlin'   # Switch to Berlin
./windows_ctl.ps1 -Command 'obstacle 1 1'   # Place obstacle
./windows_ctl.ps1 -Command 'obstacle clear' # Remove obstacles
./windows_ctl.ps1 -Command livery           # Reload livery
./windows_ctl.ps1 -Command slip             # Raw wheel slip data
```

Use `-control-port 4580` and `-mapview-port 4581` launch arguments to choose
the API and Mapping receiver ports. The bridge uses the simulator's own `-ip`
and `-port` arguments. All existing command names and map-view wire messages
remain supported. See the main README for the full control protocol.

Configure the ROS map-view sender to reach the Windows host on port 4581.
For XInput pads, use X mode on Logitech F710/F310 controllers. The first
connected pad is selected, and manual driving retains the original extension's
power, brake/reverse, and assist logic.

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `Assembly-CSharp.dll` not generated | You used the **Mono** BepInEx build instead of **IL2CPP**. Re-download the correct one. |
| `NU1100: Unable to resolve Microsoft.NETCore.App.Ref` | Run `dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org` |
| `Plugin build failed` | Ensure .NET 8 SDK is on your PATH (`dotnet --list-sdks`) and interop DLLs exist |
| Port 4580 connection refused | The simulator needs ~80s to start. Wait for the game window to fully load. |
| `winget install` of .NET SDK fails (exit code 50) | Use the `dotnet-install.ps1` script instead (see Step 4) |
| BepInEx console doesn't appear | Check `doorstop_config.ini` has `enabled = true` and `target_assembly = BepInEx\core\BepInEx.Unity.IL2CPP.dll` |

---

## Per-wheel Slip HUD

Open the right HUD with the top-right button, then choose **Slip** beside
Ground Truth and Mapping. The expanded panel shows front left/front right above
rear left/rear right. Each wheel has a current dot, three seconds of history,
and raw lateral/longitudinal slip values.
Each value also shows `abs(raw slip) / configured peak` in parentheses, matching
the native-source calculation in the ROS stack's `docs/tire_lap_report.md`.
Plot coordinates retain their signs; the reported magnitude ratios are unsigned.

The horizontal axis is lateral slip divided by `sidewaysFriction.extremumSlip`;
the vertical axis is longitudinal slip divided by `forwardFriction.extremumSlip`.
Both axes start at -2 to +2 and automatically expand in powers of two to fit the
recent samples. All wheels share the same scales, shown below the charts. Scales
are held for five seconds before shrinking; dashed lines mark +/-1. These are peak-slip
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

The read-only control command `slip` returns raw slip, friction-curve peak coordinates, wheel RPM, and vehicle speed for diagnosing chart readings. Auto-ranging changes only the plotted scale; it does not smooth or alter the slip values.
