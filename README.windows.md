# Windows IL2CPP wheel-slip telemetry

The 2026 IROS Windows practice simulator is an **IL2CPP** build: it has
`GameAssembly.dll` and `Data/il2cpp_data`, with no `Data/Managed` directory.
The macOS `TrackSelect.dll` cannot load in it. This fork's `track_select_windows/`
project is a separate BepInEx 6 plugin for that Windows build. It currently adds
four-wheel slip to the simulator's existing Socket.IO `Bridge` frame; track
selection, livery, UI, and other macOS plugin features have not been ported.

## Install and build

Work on a **copy** of the practice simulator. Download the official
[BepInEx Unity IL2CPP Windows x64 build](https://builds.bepinex.dev/projects/bepinex_be)
and extract its contents into the copy's directory, beside
`AutoDRIVE Simulator.exe`. This was tested with build
`BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.791+b782da4.zip`.

Run the copied simulator once, then close it. BepInEx creates
`BepInEx/interop/` with generated bindings for that exact simulator build.
Verify it contains `Assembly-CSharp.dll` and `UnityEngine.VehiclesModule.dll`.

Build with a .NET 8 SDK (the plugin itself targets .NET 6, as required by the
loader):

```bash
dotnet build track_select_windows/TrackSelect.Windows.csproj -c Release \
  -p:BepInExDir="<simulator copy>/BepInEx"
```

Copy `track_select_windows/bin/Release/net6.0/TrackSelect.Windows.dll` to
`<simulator copy>/BepInEx/plugins/`, then start the copied simulator. Its
`Player.log` should contain one `[WheelSlip] first frame:` line. No changes
to the stock simulator's files are required.

## Telemetry format

The plugin patches the simulator's `SocketIOComponent.Emit("Bridge", data)`
call and appends `V1 Wheel Slip` before Socket.IO serializes the frame. The
field contains four triples in **front left, front right, rear left, rear
right** order:

```
grounded forwardSlip sidewaysSlip grounded forwardSlip sidewaysSlip ...
```

`grounded` is `1` if `WheelCollider.GetGroundHit` returned a contact and `0`
otherwise. An ungrounded wheel is encoded as `0 0 0` (slip unavailable, not
zero slip). `forwardSlip` and `sidewaysSlip` are Unity's own `WheelHit`
values, formatted using invariant culture. The matching ROS bridge experiment
publishes them on `/autodrive/roboracer_1/wheel_slip` when
`publish_wheel_slip:=true`.

## Test status and limits

Built with 0 warnings/errors against generated bindings from the installed
Windows `2026-iros` practice simulator. On a separate copy started with
`-batchmode -nographics`, the plugin loaded and logged four grounded wheels
with near-zero slip at rest. This verifies the loader, patch, wheel references,
and native `GetGroundHit` access. A driving run and ROS bag capture are still
needed to validate changing slip values and end-to-end delivery.

The generated `WheelCollider.GetGroundHit(out WheelHit)` C# binding for this
build corrupted the stack in our first test. The plugin calls the same native
method with correctly sized `WheelHit` storage instead. This workaround is
specific to the tested IL2CPP bindings; rebuild and retest against any other
simulator or BepInEx version.
