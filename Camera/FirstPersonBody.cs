using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace BetterCamera.Camera;

/// <summary>
/// Hides the local player's body (and whatever they wear or hold) from the camera in first person, so it doesn't
/// block the view. Its shadow stays. Equipment can be swapped while in first person, so new renderers are picked
/// up every frame; everything is put back as it was when leaving first person.
/// </summary>
internal static class FirstPersonBody
{
    private static readonly Dictionary<Renderer, ShadowCastingMode> Hidden = [];
    private static readonly List<Renderer> Renderers = [];

    public static void Update(Player player, bool firstPerson)
    {
        if (!firstPerson || player == null || player.m_visual == null)
        {
            Restore();
            return;
        }

        player.m_visual.GetComponentsInChildren(includeInactive: true, Renderers);
        foreach (Renderer renderer in Renderers)
        {
            if (!Hidden.ContainsKey(renderer))
            {
                Hidden[renderer] = renderer.shadowCastingMode;
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    public static void Restore()
    {
        if (Hidden.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<Renderer, ShadowCastingMode> entry in Hidden)
        {
            if (entry.Key != null)
            {
                entry.Key.shadowCastingMode = entry.Value;
            }
        }

        Hidden.Clear();
    }
}
