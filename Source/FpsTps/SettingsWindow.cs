using UnityEngine;
using Verse;

namespace Anno.FpsTps;

internal sealed class SettingsWindow
{
    private readonly Listing_Standard list = new();

    internal void Draw(Rect area)
    {
        FpsTpsSettings settings = FpsTpsMod.Settings;
        bool oldFps = settings.ShowFps;
        bool oldTps = settings.ShowTps;
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Color oldColor = GUI.color;
        try
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            list.Begin(area);
            list.CheckboxLabeled("AT_ShowFps".Translate(), ref settings.ShowFps);
            list.CheckboxLabeled("AT_ShowTps".Translate(), ref settings.ShowTps);
            list.End();
        }
        finally
        {
            GUI.color = oldColor;
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
        }
        if (oldFps != settings.ShowFps || oldTps != settings.ShowTps)
            CounterOverlay.Reset();
    }
}
