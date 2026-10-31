using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace TrackSelect.Windows;

[BepInPlugin("uiuc.autodrive.windows", "AutoDRIVE Windows Plugin", "0.1.0")]
public sealed class WindowsPlugin : BasePlugin
{
    public override void Load()
    {
        Log.LogInfo("Windows IL2CPP plugin loaded");
    }
}
