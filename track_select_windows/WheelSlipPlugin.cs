using System.Globalization;
using System.Text;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using SocketIO;
using UnityEngine;

namespace TrackSelect.Windows;

[BepInPlugin("uiuc.autodrive.wheel-slip", "AutoDRIVE Wheel Slip", "0.1.0")]
public sealed class WheelSlipPlugin : BasePlugin
{
    public override void Load()
    {
        new Harmony("uiuc.autodrive.wheel-slip").PatchAll(typeof(WheelSlipPlugin).Assembly);
        Log.LogInfo("Wheel slip telemetry patch installed");
    }
}

// Socket.EmitTelemetry constructs the dictionary and calls socket.Emit("Bridge", data)
// on Unity's main thread. Patching that exact overload adds a field to the same
// frame as the simulator's IMU and encoders, before Socket.IO serializes it.
[HarmonyPatch(typeof(SocketIOComponent), nameof(SocketIOComponent.Emit),
    new[] { typeof(string), typeof(JSONObject) })]
internal static class BridgeEmitPatch
{
    private static VehicleController? vehicle;
    private static bool loggedFirstFrame;
    private static bool loggedError;
    private static readonly IntPtr GetGroundHitMethod =
        (IntPtr)(typeof(WheelCollider).GetField(
            "NativeMethodInfoPtr_GetGroundHit_Public_Boolean_byref_WheelHit_0",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        )?.GetValue(null) ?? throw new MissingFieldException("WheelCollider.GetGroundHit binding"));

    private static void Prefix(string ev, JSONObject data)
    {
        if (ev != "Bridge" || data == null || data.HasField("V1 Wheel Slip")) return;
        try
        {
            // The 2026 RoboRacer practice build has one VehicleController.
            if (vehicle == null)
                vehicle = UnityEngine.Object.FindObjectOfType<VehicleController>();
            if (vehicle == null) return;

            WheelCollider?[] wheels =
            {
                vehicle.FrontLeftWheelCollider, vehicle.FrontRightWheelCollider,
                vehicle.RearLeftWheelCollider, vehicle.RearRightWheelCollider
            };
            if (Array.Exists(wheels, wheel => wheel == null))
                throw new InvalidOperationException("RoboRacer wheel collider reference is missing");
            var value = new StringBuilder(96);
            foreach (WheelCollider? wheel in wheels)
            {
                if (value.Length != 0) value.Append(' ');
                if (GetGroundHitSafe(wheel!, out WheelHit hit))
                {
                    value.Append("1 ");
                    value.Append(hit.forwardSlip.ToString("R", CultureInfo.InvariantCulture));
                    value.Append(' ');
                    value.Append(hit.sidewaysSlip.ToString("R", CultureInfo.InvariantCulture));
                }
                else value.Append("0 0 0");
            }
            data.AddField("V1 Wheel Slip", value.ToString());
            if (!loggedFirstFrame)
            {
                UnityEngine.Debug.Log("[WheelSlip] first frame: " + value);
                loggedFirstFrame = true;
            }
        }
        catch (Exception ex)
        {
            // A telemetry feature must never block simulator control frames.
            if (!loggedError)
            {
                UnityEngine.Debug.LogWarning("[WheelSlip] telemetry unavailable: " + ex);
                loggedError = true;
            }
        }
    }

    // The generated IL2CPP GetGroundHit(out WheelHit) wrapper in this build
    // reserves only an IntPtr for an out value struct. Native Unity writes the
    // larger WheelHit into that slot, corrupting the stack. Pass the unboxed
    // storage of a real WheelHit instead. This is specific to the 2026-iros
    // Windows interop bindings and should be removed if the generator is fixed.
    private static unsafe bool GetGroundHitSafe(WheelCollider wheel, out WheelHit hit)
    {
        hit = new WheelHit();
        IntPtr storage = IL2CPP.il2cpp_object_unbox(
            IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)hit));
        void** args = stackalloc void*[1];
        args[0] = (void*)storage;
        IntPtr exception = IntPtr.Zero;
        IntPtr result = IL2CPP.il2cpp_runtime_invoke(GetGroundHitMethod,
            IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)wheel), args, ref exception);
        if (exception != IntPtr.Zero || result == IntPtr.Zero)
            throw new InvalidOperationException("Native WheelCollider.GetGroundHit failed");
        return *(bool*)IL2CPP.il2cpp_object_unbox(result);
    }
}
