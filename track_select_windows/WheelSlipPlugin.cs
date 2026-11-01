using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using SocketIO;

namespace TrackSelect.Windows;

[BepInPlugin("uiuc.autodrive.wheel-slip", "AutoDRIVE Wheel Slip", "0.2.0")]
public sealed class WheelSlipPlugin : BasePlugin
{
    public override void Load()
    {
        new Harmony("uiuc.autodrive.wheel-slip").PatchAll(typeof(WheelSlipPlugin).Assembly);
        Log.LogInfo("Wheel slip telemetry and HUD enabled");
    }
}

[HarmonyPatch(typeof(SocketIOComponent), nameof(SocketIOComponent.Emit), new[] { typeof(string), typeof(JSONObject) })]
internal static class BridgeEmitPatch
{
    private static bool warned;
    private static void Prefix(string ev, JSONObject data)
    {
        if (ev != "Bridge" || data == null || data.HasField("V1 Wheel Slip")) return;
        try
        {
            var samples = WheelSlipData.Read();
            if (samples != null) data.AddField("V1 Wheel Slip", WheelSlipData.Encode(samples));
        }
        catch (System.Exception e)
        {
            if (!warned) UnityEngine.Debug.LogWarning("[WheelSlip] " + e.Message);
            warned = true;
        }
    }
}
