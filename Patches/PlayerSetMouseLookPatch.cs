using BetterCamera.FirstPerson;

using HarmonyLib;

using UnityEngine;

namespace BetterCamera.Patches;

/// <summary>
/// Limits looking around in first person, so the camera doesn't see inside the player's armour and body:
/// <list type="bullet">
/// <item>Looking down stops at Max Look Down (vanilla allows 89 degrees). Looking up is unchanged.</item>
/// <item>While seated (at the helm, on a chair or riding), the body can't turn, so looking around is limited like
/// in a real seat: to the side up to Seated Turn Angle, enough to look over either shoulder, and down to Seated
/// Max Look Down while facing forward. Turned to the side, the body is below the camera, so the look-down limit
/// eases up to Seated Max Look Down Sideways, lifting the view as you turn instead of looking into the body. The
/// limits follow the seat as the ship or mount turns, as this runs every frame.</item>
/// </list>
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.SetMouseLook))]
internal static class Player_SetMouseLook_Patch
{
    /// <summary>
    /// Degrees to the side of the seat where the seated look-down limit starts to ease up, and where it reaches
    /// the sideways limit.
    /// </summary>
    private const float SidewaysStart = 30f;

    private const float SidewaysEnd = 100f;

    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (__instance != Player.m_localPlayer || !FirstPersonSettings.Active)
        {
            return;
        }

        bool seated = __instance.IsAttached() || __instance.IsRiding();
        bool changed = false;
        float maxDown = FirstPersonSettings.MaxLookDown;

        if (seated)
        {
            float body = __instance.transform.rotation.eulerAngles.y;
            float offset = Mathf.DeltaAngle(body, __instance.m_lookYaw.eulerAngles.y);
            float limit = FirstPersonSettings.SeatedTurnAngle;
            if (Mathf.Abs(offset) > limit)
            {
                offset = Mathf.Sign(offset) * limit;
                __instance.m_lookYaw = Quaternion.Euler(0f, body + offset, 0f);
                changed = true;
            }

            float sideways = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(SidewaysStart, SidewaysEnd, Mathf.Abs(offset)));
            maxDown = Mathf.Min(maxDown,
                Mathf.Lerp(FirstPersonSettings.SeatedMaxLookDown, FirstPersonSettings.SeatedMaxLookDownSideways, sideways));
        }

        // Positive pitch looks down.
        if (__instance.m_lookPitch > maxDown)
        {
            __instance.m_lookPitch = maxDown;
            changed = true;
        }

        if (changed)
        {
            __instance.UpdateEyeRotation();
            __instance.m_lookDir = __instance.m_eye.forward;
        }
    }
}
