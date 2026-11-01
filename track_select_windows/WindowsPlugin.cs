using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace TrackSelect.Windows;

[BepInPlugin("uiuc.autodrive.windows", "AutoDRIVE Windows Plugin", "0.1.0")]
public sealed class WindowsPlugin : BasePlugin
{
    public override void Load()
    {
        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<TrackSelect.SimControl>();
        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<TrackSelect.MapView>();
        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<TrackSelect.MapDrag>();
        Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<TrackSelect.Gamepad>();
        TrackSelect.TrackSelector.Init();
        Log.LogInfo("Windows extensions loaded");
    }
}
