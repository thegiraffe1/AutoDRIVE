# Windows IL2CPP plugin foundation

The 2026 IROS Windows practice simulator is an **IL2CPP** build: it has
`GameAssembly.dll` and `Data/il2cpp_data`, with no `Data/Managed` directory.
The macOS `TrackSelect.dll` cannot load in it. This branch adds a separate
BepInEx 6 Unity IL2CPP plugin project for Windows. It establishes the loader and
build setup; game features are added in follow-on branches.

## Install and verify the loader

Work on a **copy** of the practice simulator. Download the official
[BepInEx Unity IL2CPP Windows x64 build](https://builds.bepinex.dev/projects/bepinex_be)
and extract its contents into the copy's directory, beside
`AutoDRIVE Simulator.exe`. This was tested with build
`BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.791+b782da4.zip`.

Run the copied simulator once, then close it. BepInEx creates
`BepInEx/interop/` with generated bindings for that exact simulator build.
The loader log should report `Chainloader startup complete`.

## Build the plugin foundation

Use a .NET 8 SDK. The Windows IL2CPP loader uses .NET 6:

```bash
dotnet build track_select_windows/TrackSelect.Windows.csproj -c Release \
  -p:BepInExDir="<simulator copy>/BepInEx"
```

Copy `track_select_windows/bin/Release/net6.0/TrackSelect.Windows.dll` to
`<simulator copy>/BepInEx/plugins/`, then start the copied simulator. The
`BepInEx/LogOutput.log` file should report `Windows IL2CPP plugin loaded`.

This is a separate runtime path from the macOS managed plugin; the existing
multi-track features have not been ported here.
