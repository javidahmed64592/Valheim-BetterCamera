using System.Collections.Generic;

using UnityEngine;

namespace BetterCamera.FirstPerson;

/// <summary>
/// The local player's body in first person. Only the head is hidden, by shrinking the head bone after the
/// animation each frame. Hair, beard and helmet are attached to it, so they go too, and the arms and gear keep the
/// game's own animations. Lights worn on the head (such as the Dvergr circlet's) point where you look. Everything
/// is put back as it was when leaving first person.
/// </summary>
internal static class FirstPersonBody
{
    private static readonly Vector3 ShrunkScale = Vector3.one * 0.0001f;


    private static Transform _head;
    private static Vector3 _headScale;

    /// <summary>
    /// Lights on the helmet slot that are being aimed, with their own rotation to put back.
    /// </summary>
    private static readonly Dictionary<Light, Quaternion> AimedLights = [];
    private static readonly List<Light> Lights = [];

    public static void Update(Player player, bool firstPerson)
    {
        if (!firstPerson || player == null || player.m_visual == null)
        {
            Restore();
            return;
        }

        AimHeadLights(player);

        ShrinkHead(player.m_head);
    }

    /// <summary>
    /// Points every light on the helmet slot (such as the Dvergr circlet's) along the camera's view, so it shines
    /// where the crosshair is. The head bone itself isn't turned: it's hidden, and its resting angle depends on
    /// the animation, so a light carried by it would point somewhere else depending on when first person started.
    /// </summary>
    private static void AimHeadLights(Player player)
    {
        GameObject helmet = player.m_visEquipment != null ? player.m_visEquipment.m_helmetItemInstance : null;
        UnityEngine.Camera camera = Utils.GetMainCamera();
        if (helmet == null || camera == null)
        {
            RestoreLights();
            return;
        }

        helmet.GetComponentsInChildren(includeInactive: false, Lights);
        foreach (Light light in Lights)
        {
            if (!AimedLights.ContainsKey(light))
            {
                AimedLights[light] = light.transform.localRotation;
            }

            light.transform.rotation = camera.transform.rotation;
        }
    }

    private static void RestoreLights()
    {
        foreach (KeyValuePair<Light, Quaternion> entry in AimedLights)
        {
            if (entry.Key != null)
            {
                entry.Key.transform.localRotation = entry.Value;
            }
        }

        AimedLights.Clear();
    }

    public static void Restore()
    {
        RestoreHead();
        RestoreLights();
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
}
