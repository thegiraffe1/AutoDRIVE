using System;
using System.Collections.Generic;
using TrackSelect.Windows;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace TrackSelect;

public partial class MapView
{
    static bool slipTab;
    Image slipTabButton;
    RectTransform slipRoot;
    Text slipFooter;
    GameObject slipBackdrop;
    int originalPanelOrder;
    Texture2D slipTexture;
    readonly Color32[] slipPixels = new Color32[512 * 512];
    readonly Queue<(float time, Vector2 point)>[] slipHistory = {
        new(), new(), new(), new()
    };
    readonly Text[] slipLabels = new Text[4];
    readonly string[] wheelNames = { "Front left", "Front right", "Rear left", "Rear right" };
    readonly Color32[] wheelColors = { new(68, 210, 255, 255), new(255, 179, 71, 255),
        new(114, 225, 135, 255), new(222, 145, 255, 255) };
    WheelSlipReading[] slipSamples;
    float nextSlipSample;
    float slipRangeX = 2, slipRangeY = 2, scaleHoldUntil;

    [HideFromIl2Cpp]
    void ResetSlipHistory()
    {
        foreach (var q in slipHistory) q.Clear();
        slipSamples = null;
        nextSlipSample = 0;
        slipRangeX = slipRangeY = 2;
        scaleHoldUntil = 0;
        if (slipTexture != null) Destroy(slipTexture);
        slipTexture = null;
    }

    [HideFromIl2Cpp]
    void BuildSlipHud(Transform parent, RectTransform source, Text style)
    {
        originalPanelOrder = parent.GetSiblingIndex();
        var backdrop = NewRect("Slip Backdrop", parent, Vector2.zero, Vector2.one);
        backdrop.gameObject.AddComponent<Image>().color = new Color32(21, 26, 31, 255);
        backdrop.SetAsFirstSibling();
        slipBackdrop = backdrop.gameObject;
        slipBackdrop.SetActive(false);
        slipRoot = NewRect("Slip Charts", parent, source.anchorMin, source.anchorMax);
        slipRoot.offsetMin = source.offsetMin;
        slipRoot.offsetMax = source.offsetMax;
        var raw = slipRoot.gameObject.AddComponent<RawImage>();
        raw.raycastTarget = false;
        slipTexture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        slipTexture.filterMode = FilterMode.Bilinear;
        raw.texture = slipTexture;
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2) * .5f, y = i < 2 ? .5f : 0;
            var rt = NewRect(wheelNames[i], slipRoot, new Vector2(x, y + .36f), new Vector2(x + .5f, y + .5f));
            var t = rt.gameObject.AddComponent<Text>();
            t.font = style.font; t.fontSize = 16; t.color = wheelColors[i];
            t.alignment = TextAnchor.MiddleCenter; t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 10; t.resizeTextMaxSize = 20; t.raycastTarget = false;
            slipLabels[i] = t;
        }
        slipRoot.gameObject.SetActive(false);
        var foot = NewRect("Slip Axes", parent, Vector2.zero, new Vector2(1, .10f));
        slipFooter = foot.gameObject.AddComponent<Text>();
        slipFooter.font = style.font; slipFooter.fontSize = 12; slipFooter.color = Color.white;
        slipFooter.alignment = TextAnchor.MiddleCenter; slipFooter.resizeTextForBestFit = true;
        slipFooter.resizeTextMinSize = 6; slipFooter.resizeTextMaxSize = 12;
        slipFooter.raycastTarget = false;
        slipFooter.text = "|slip|/peak: x lateral; y longitudinal\nRange 0-2; dashed lines mark 1";
        slipFooter.gameObject.SetActive(false);
    }

    [HideFromIl2Cpp]
    void TickSlipHud()
    {
        if (Time.time < nextSlipSample) return;
        nextSlipSample = Time.time + .05f;
        try { slipSamples = WheelSlipData.Read(); }
        catch { slipSamples = null; }
        for (int i = 0; i < 4; i++)
        {
            var q = slipHistory[i];
            if (slipSamples == null || !slipSamples[i].Grounded) q.Clear();
            else
            {
                var s = slipSamples[i];
                if (float.IsFinite(s.Sideways) && float.IsFinite(s.Forward) && s.SidewaysPeak > 0 && s.ForwardPeak > 0)
                    q.Enqueue((Time.time, new Vector2(s.Sideways / s.SidewaysPeak, s.Forward / s.ForwardPeak)));
            }
            while (q.Count > 0 && (Time.time - q.Peek().time > 3f || q.Count > 64)) q.Dequeue();
        }
        float wantedX = 2, wantedY = 2;
        foreach (var q in slipHistory)
            foreach (var item in q)
            {
                while (wantedX < Mathf.Abs(item.point.x) * 1.1f && wantedX < 32768) wantedX *= 2;
                while (wantedY < Mathf.Abs(item.point.y) * 1.1f && wantedY < 32768) wantedY *= 2;
            }
        if (wantedX > slipRangeX || wantedY > slipRangeY)
        {
            slipRangeX = Mathf.Max(slipRangeX, wantedX);
            slipRangeY = Mathf.Max(slipRangeY, wantedY);
            scaleHoldUntil = Time.time + 5;
        }
        else if (Time.time > scaleHoldUntil)
        {
            slipRangeX = wantedX; slipRangeY = wantedY;
            scaleHoldUntil = Time.time + 5;
        }
        if (slipTab) DrawSlipHud();
    }

    [HideFromIl2Cpp]
    void ShowSlipHud()
    {
        bool opening = slipTab && !slipRoot.gameObject.activeSelf;
        bool closing = !slipTab && slipRoot.gameObject.activeSelf;
        if (opening) panel.transform.SetAsLastSibling();
        if (closing) panel.transform.SetSiblingIndex(originalPanelOrder);
        view.gameObject.SetActive(!slipTab);
        slipRoot.gameObject.SetActive(slipTab);
        slipBackdrop.SetActive(slipTab);
        slipFooter.gameObject.SetActive(slipTab);
        // Enlarge the charts into the lap-time area only while this tab is selected.
        panel.transform.Cast<RectTransform>().anchorMin = new Vector2(.04f, slipTab ? .015f : .30f);
        gtTab.color = !slipTab && !mappingTab ? TabOn : TabOff;
        mapTab.color = !slipTab && mappingTab ? TabOn : TabOff;
        slipTabButton.color = slipTab ? TabOn : TabOff;
        if (opening) DrawSlipHud();
    }

    [HideFromIl2Cpp]
    void DrawSlipHud()
    {
        Array.Fill(slipPixels, new Color32(21, 26, 31, 255));
        for (int i = 0; i < 4; i++)
        {
            int ox = (i % 2) * 256, oy = i < 2 ? 256 : 0;
            // Shared magnitude axes across all wheels. Dashed +1 lines are
            // separate friction-curve coordinates, not a combined force boundary.
            for (int p = 16; p <= 240; p++)
            {
                Put(ox + p, oy + 16, new Color32(110, 118, 126, 255));
                Put(ox + 16, oy + 16 + (p - 16) * 160 / 224, new Color32(110, 118, 126, 255));
                if (p % 8 < 4)
                {
                    Put(ox + p, oy + 16 + Mathf.RoundToInt(160 / slipRangeY), new Color32(110, 118, 126, 255));
                    Put(ox + 16 + Mathf.RoundToInt(224 / slipRangeX), oy + 16 + (p - 16) * 160 / 224, new Color32(110, 118, 126, 255));
                }
            }
            foreach (var sample in slipHistory[i])
            {
                float fade = Mathf.Clamp01(1f - (Time.time - sample.time) / 3f);
                var c = wheelColors[i];
                c.r = (byte)(c.r * (.2f + .8f * fade)); c.g = (byte)(c.g * (.2f + .8f * fade)); c.b = (byte)(c.b * (.2f + .8f * fade));
                Dot(sample.point, c, 1);
            }
            if (slipSamples == null) slipLabels[i].text = wheelNames[i] + "\nunavailable";
            else if (!slipSamples[i].Grounded) slipLabels[i].text = wheelNames[i] + "\nno ground contact";
            else
            {
                var s = slipSamples[i];
                var point = new Vector2(s.Sideways / Mathf.Max(s.SidewaysPeak, 1e-6f), s.Forward / Mathf.Max(s.ForwardPeak, 1e-6f));
                bool clipped = Mathf.Abs(point.x) > slipRangeX || Mathf.Abs(point.y) > slipRangeY;
                Dot(point, clipped ? new Color32(255, 80, 80, 255) : wheelColors[i], 3);
                slipLabels[i].text = $"{wheelNames[i]}{(clipped ? " [off scale]" : "")}\nlat {s.Sideways:0.000} ({Mathf.Abs(point.x):0.0}x)\nlong {s.Forward:0.000} ({Mathf.Abs(point.y):0.0}x)";
            }
            [HideFromIl2Cpp]
            void Dot(Vector2 p, Color32 color, int radius)
            {
                int x = ox + 16 + Mathf.RoundToInt(Mathf.Clamp01(Mathf.Abs(p.x) / slipRangeX) * 224);
                int y = oy + 16 + Mathf.RoundToInt(Mathf.Clamp01(Mathf.Abs(p.y) / slipRangeY) * 160);
                for (int dx = -radius; dx <= radius; dx++)
                    for (int dy = -radius; dy <= radius; dy++) Put(x + dx, y + dy, color);
            }
        }
        slipTexture.SetPixels32(slipPixels); slipTexture.Apply(false);
        slipFooter.text = $"|slip|/peak: x lateral 0-{slipRangeX:0}; y long 0-{slipRangeY:0}\nDashed lines: 1. Numbers: raw (ratio).";
        [HideFromIl2Cpp]
        void Put(int x, int y, Color32 color)
        { if (x >= 0 && x < 512 && y >= 0 && y < 512) slipPixels[y * 512 + x] = color; }
    }
}
