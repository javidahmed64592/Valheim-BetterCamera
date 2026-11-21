using BetterCamera.FirstPerson;

using HarmonyLib;

using UnityEngine;

namespace BetterCamera.Patches;

/// <summary>
/// Limits looking around in first person, so the camera doesn't see inside the player's armour and body:
/// <list type="bullet">
/// <item>Looking down stops at Max Look Down (vanilla allows 89 degrees). Looking up is unchanged.</item>
/// <item>While seated (at the helm, on a chair or riding), the body can't turn, so looking around is limited like
/// in a real seat: down to Seated Max Look Down, and to the side up to Seated Turn Angle, enough to look over
/// either shoulder. The limit follows the seat as the ship or mount turns, as this runs every frame.</item>
/// </list>
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.SetMouseLook))]
internal static class Player_SetMouseLook_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (__instance != Player.m_localPlayer || !FirstPersonSettings.Active)
        {
            return;
        }

        bool seated = __instance.IsAttached() || __instance.IsRiding();
        bool changed = false;

        // Positive pitch looks down.
        float maxDown = seated ? Mathf.Min(FirstPersonSettings.SeatedMaxLookDown, FirstPersonSettings.MaxLookDown) : FirstPersonSettings.MaxLookDown;
        if (__instance.m_lookPitch > maxDown)
        {
            __instance.m_lookPitch = maxDown;
            changed = true;
        }

        if (seated)
        {
            float body = __instance.transform.rotation.eulerAngles.y;
            float look = __instance.m_lookYaw.eulerAngles.y;
            float offset = Mathf.DeltaAngle(body, look);
            float limit = FirstPersonSettings.SeatedTurnAngle;
            if (Mathf.Abs(offset) > limit)
            {
                __instance.m_lookYaw = Quaternion.Euler(0f, body + (Mathf.Sign(offset) * limit), 0f);
                changed = true;
            }
        }

        if (changed)
        {
            __instance.UpdateEyeRotation();
            __instance.m_lookDir = __instance.m_eye.forward;
        }
    }
}
