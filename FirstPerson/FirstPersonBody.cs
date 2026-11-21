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

    /// <summary>
    /// The head as the animation last posed it, the rotation written over it, and how the head sat relative to
    /// the body when first person started.
    /// </summary>
    private static Transform _turnedHead;
    private static Quaternion _animatedRotation;
    private static Quaternion _writtenRotation;
    private static Quaternion _headToBody;

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
    /// Points the head exactly where the player looks. Adding the look on top of the animation overshot, as some
    /// animations already lean the upper body towards the view; the head is hidden, so its animation isn't
    /// needed. How the head sits relative to the body when first person starts gives its forward direction.
    /// </summary>
    private static void TurnHead(Player player)
    {
        Transform head = player.m_head;
        if (head == null || player.m_eye == null)
        {
            return;
        }

        if (head != _turnedHead)
        {
            _turnedHead = head;
            _headToBody = Quaternion.Inverse(player.transform.rotation) * head.rotation;
        }

        // Remember the animation's pose, to put back when leaving first person.
        if (head.localRotation != _writtenRotation)
        {
            _animatedRotation = head.localRotation;
        }

        head.rotation = player.m_eye.rotation * _headToBody;
        _writtenRotation = head.localRotation;
    }

    /// <summary>
    /// Puts the head back to its animated pose if the animation hasn't already.
    /// </summary>
    private static void RestoreHeadTurn()
    {
        if (_turnedHead != null && _turnedHead.localRotation == _writtenRotation)
        {
            _turnedHead.localRotation = _animatedRotation;
        }

        _turnedHead = null;
    }

    public static void Restore()
    {
        ShowRenderers();
        RestoreHead();
        RestoreHeadTurn();
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
