param([Parameter(Mandatory=$true)][string]$SimulatorDir)
$ErrorActionPreference = 'Stop'
$sim = (Resolve-Path $SimulatorDir).Path
$bep = Join-Path $sim 'BepInEx'
if (!(Test-Path (Join-Path $bep 'interop/Assembly-CSharp.dll'))) {
    throw 'Install BepInEx 6 Unity IL2CPP x64 into a simulator copy and run it once first.'
}
dotnet build (Join-Path $PSScriptRoot 'track_select_windows/TrackSelect.Windows.csproj') -c Release "-p:BepInExDir=$bep"
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed' }
New-Item -ItemType Directory -Force (Join-Path $bep 'plugins') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'track_select_windows/bin/Release/net6.0/TrackSelect.Windows.dll') (Join-Path $bep 'plugins') -Force
$assets = Join-Path $sim 'Data/StreamingAssets'
New-Item -ItemType Directory -Force $assets | Out-Null
foreach ($name in @('tracks','livery')) {
    Copy-Item (Join-Path $PSScriptRoot $name) $assets -Recurse -Force
}
Write-Host "Installed Windows extensions into $sim"
