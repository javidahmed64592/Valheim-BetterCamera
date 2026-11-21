using System.Collections.Generic;

using UnityEngine;

namespace BetterCamera.Photo;

/// <summary>
/// The parts of the HUD that are always hidden in photo mode: the hotbar, the minimap, the health, stamina, eitr
/// and adrenaline bars, and the ship and mount panels. They're shrunk to nothing rather than turned off, as the
/// game turns several of them on and off itself every frame. Each is put back as it was on leaving photo mode.
/// </summary>
internal static class PhotoHud
{
    private static readonly Dictionary<Transform, Vector3> Hidden = [];
    private static readonly List<Transform> Parts = [];

    /// <summary>
    /// Called every frame in photo mode, after the HUD has updated.
    /// </summary>
    public static void Hide()
    {
        Collect();
        foreach (Transform part in Parts)
        {
            if (part == null)
            {
                continue;
            }

            if (!Hidden.ContainsKey(part))
            {
                Hidden[part] = part.localScale;
            }

            part.localScale = Vector3.zero;
        }
    }

    public static void Restore()
    {
        foreach (KeyValuePair<Transform, Vector3> entry in Hidden)
        {
            if (entry.Key != null)
            {
                entry.Key.localScale = entry.Value;
            }
        }

        Hidden.Clear();
    }

    private static void Collect()
    {
        Parts.Clear();
        Hud hud = Hud.instance;
        if (hud != null)
        {
            Parts.Add(hud.m_healthPanel);
            Parts.Add(hud.m_staminaBar2Root);
            Parts.Add(hud.m_eitrBarRoot);
            Parts.Add(hud.m_adrenalineBarRoot);
            Parts.Add(hud.m_shipHudRoot != null ? hud.m_shipHudRoot.transform : null);
            Parts.Add(hud.m_mountPanel != null ? hud.m_mountPanel.transform : null);
            if (hud.m_rootObject != null)
            {
                foreach (HotkeyBar bar in hud.m_rootObject.GetComponentsInChildren<HotkeyBar>(includeInactive: true))
                {
                    Parts.Add(bar.transform);
                }
            }
        }

        if (Minimap.instance != null && Minimap.instance.m_smallRoot != null)
        {
            Parts.Add(Minimap.instance.m_smallRoot.transform);
        }
    }
}
