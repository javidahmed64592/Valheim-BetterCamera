using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace BetterCamera.FirstPerson;

/// <summary>
/// The local player's body in first person.
/// <list type="bullet">
/// <item>Body view: only the head is hidden, by shrinking the head bone after the animation each frame. Hair,
/// beard and helmet are attached to it, so they go too, and the arms and gear keep the game's own
/// animations.</item>
/// <item>Held item view: the whole body (and whatever is worn or held) is hidden from the camera, keeping its
/// shadow. Equipment can change meanwhile, so new renderers are picked up every frame.</item>
/// </list>
/// Everything is put back as it was when leaving first person or switching view.
/// </summary>
internal static class FirstPersonBody
{
    private static readonly Vector3 ShrunkScale = Vector3.one * 0.0001f;

    private static readonly Dictionary<Renderer, ShadowCastingMode> Hidden = [];
    private static readonly List<Renderer> Renderers = [];

    private static Transform _head;
    private static Vector3 _headScale;

    public static void Update(Player player, bool firstPerson)
    {
        if (!firstPerson || player == null || player.m_visual == null)
        {
            Restore();
            return;
        }

        if (FirstPersonSettings.Gear == GearView.Body)
        {
            ShowRenderers();
            ShrinkHead(player.m_head);
        }
        else
        {
            RestoreHead();
            HideRenderers(player.m_visual);
        }
    }

    public static void Restore()
    {
        ShowRenderers();
        RestoreHead();
    }

    private static void ShrinkHead(Transform head)
    {
        if (head == null)
        {
            return;
        }

        if (_head != head)
        {
            RestoreHead();
            _head = head;
            _headScale = head.localScale;
        }

        head.localScale = ShrunkScale;
    }

    private static void RestoreHead()
    {
        if (_head != null)
        {
            _head.localScale = _headScale;
        }

        _head = null;
    }

    private static void HideRenderers(GameObject visual)
    {
        visual.GetComponentsInChildren(includeInactive: true, Renderers);
        foreach (Renderer renderer in Renderers)
        {
            if (!Hidden.ContainsKey(renderer))
            {
                Hidden[renderer] = renderer.shadowCastingMode;
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    private static void ShowRenderers()
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
