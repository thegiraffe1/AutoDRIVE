using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace TrackSelect.Windows;

internal readonly record struct WheelSlipReading(bool Grounded, float Forward, float Sideways, float ForwardPeak, float SidewaysPeak, float Rpm);

internal static class WheelSlipData
{
    private static VehicleController vehicle;
    private static readonly IntPtr Method = (IntPtr)typeof(WheelCollider).GetField(
        "NativeMethodInfoPtr_GetGroundHit_Public_Boolean_byref_WheelHit_0",
        BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

    internal static WheelSlipReading[] Read()
    {
        if (vehicle == null) vehicle = UnityEngine.Object.FindObjectOfType<VehicleController>();
        if (vehicle == null) return null;
        var wheels = new[] { vehicle.FrontLeftWheelCollider, vehicle.FrontRightWheelCollider,
                             vehicle.RearLeftWheelCollider, vehicle.RearRightWheelCollider };
        var readings = new WheelSlipReading[4];
        for (int i = 0; i < 4; i++)
        {
            if (wheels[i] == null) return null;
            bool grounded = GroundHit(wheels[i], out WheelHit hit);
            readings[i] = new WheelSlipReading(grounded, grounded ? hit.forwardSlip : 0,
                grounded ? hit.sidewaysSlip : 0, wheels[i].forwardFriction.extremumSlip,
                wheels[i].sidewaysFriction.extremumSlip, wheels[i].rpm);
        }
        return readings;
    }

    internal static string Encode(WheelSlipReading[] samples)
    {
        var text = new StringBuilder(96);
        foreach (var s in samples)
        {
            if (text.Length > 0) text.Append(' ');
            text.Append(s.Grounded ? "1 " : "0 ");
            text.Append(s.Forward.ToString("R", CultureInfo.InvariantCulture)).Append(' ');
            text.Append(s.Sideways.ToString("R", CultureInfo.InvariantCulture));
        }
        return text.ToString();
    }

    internal static string Diagnostics()
    {
        var samples = Read();
        if (samples == null) return "{\"ok\":false,\"error\":\"vehicle unavailable\"}";
        var names = new[] { "front_left", "front_right", "rear_left", "rear_right" };
        var items = new string[4];
        for (int i = 0; i < 4; i++)
        {
            var s = samples[i];
            items[i] = FormattableString.Invariant($"{{\"wheel\":\"{names[i]}\",\"grounded\":{(s.Grounded ? "true" : "false")},\"forward\":{s.Forward:R},\"sideways\":{s.Sideways:R},\"forward_peak\":{s.ForwardPeak:R},\"sideways_peak\":{s.SidewaysPeak:R},\"rpm\":{s.Rpm:R}}}");
        }
        float speed = vehicle.GetComponent<Rigidbody>().velocity.magnitude;
        return FormattableString.Invariant($"{{\"ok\":true,\"speed_mps\":{speed:R},\"wheels\":[{string.Join(",", items)}]}}");
    }

    // The generated out-WheelHit wrapper reserves only pointer-sized storage.
    // Supply the unboxed storage of a correctly allocated native WheelHit.
    private static unsafe bool GroundHit(WheelCollider wheel, out WheelHit hit)
    {
        hit = new WheelHit();
        void** args = stackalloc void*[1];
        args[0] = (void*)IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)hit));
        IntPtr exception = IntPtr.Zero;
        IntPtr result = IL2CPP.il2cpp_runtime_invoke(Method,
            IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)wheel), args, ref exception);
        if (exception != IntPtr.Zero || result == IntPtr.Zero)
            throw new InvalidOperationException("Native WheelCollider.GetGroundHit failed");
        return *(bool*)IL2CPP.il2cpp_object_unbox(result);
    }
}
