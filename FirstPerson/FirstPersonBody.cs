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
/// In both views the head turns to face where you look, so anything worn on it (such as the Dvergr circlet's
/// light) points at the crosshair. Everything is put back as it was when leaving first person or switching view.
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

        TurnHead(player);

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

    /// <summary>
    /// Adds the difference between where the body faces and where the player looks (yaw and pitch) to the head's
    /// animated rotation. The animation sets the head again every frame, so this never builds up.
    /// </summary>
    private static void TurnHead(Player player)
    {
        Transform head = player.m_head;
        if (head == null || player.m_eye == null)
        {
            return;
        }

        Quaternion look = player.m_eye.rotation * Quaternion.Inverse(player.transform.rotation);
        head.rotation = look * head.rotation;
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
