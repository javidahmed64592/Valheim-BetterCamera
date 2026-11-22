using System.Collections.Generic;

using UnityEngine;

namespace BetterCamera.Photo;

/// <summary>
/// Hides your own character in photo mode, for shots of the scenery: every renderer on it (body, gear, and
/// effects such as a torch's flame) is turned off. Gear can change while it's hidden, so new renderers are picked
/// up every frame. Everything is put back as it was when it's shown again or photo mode ends.
/// </summary>
internal static class PhotoCharacter
{
    private static readonly Dictionary<Renderer, bool> Hidden = [];
    private static readonly List<Renderer> Renderers = [];

    public static bool IsHidden { get; private set; }

    public static void Toggle()
    {
        IsHidden = !IsHidden;
        if (!IsHidden)
        {
            Restore();
        }
    }

    /// <summary>
    /// Called every frame in photo mode.
    /// </summary>
    public static void Apply(Player player)
    {
        if (!IsHidden || player == null || player.m_visual == null)
        {
            return;
        }

        player.m_visual.GetComponentsInChildren(includeInactive: true, Renderers);
        foreach (Renderer renderer in Renderers)
        {
            if (!Hidden.ContainsKey(renderer))
            {
                Hidden[renderer] = renderer.enabled;
            }

            renderer.enabled = false;
        }
    }

    /// <summary>
    /// Shows the character again and forgets the setting, so the next photo mode starts with it shown.
    /// </summary>
    public static void Reset()
    {
        IsHidden = false;
        Restore();
    }

    private static void Restore()
    {
        foreach (KeyValuePair<Renderer, bool> entry in Hidden)
        {
            if (entry.Key != null)
            {
                entry.Key.enabled = entry.Value;
            }
        }

        Hidden.Clear();
    }
}
