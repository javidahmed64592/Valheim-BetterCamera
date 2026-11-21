using BetterCamera.FirstPerson;

using HarmonyLib;

using UnityEngine;

namespace BetterCamera.Patches;

/// <summary>
/// In first person the body turns with the head: always while moving (so strafing doesn't swing it sideways),
/// and while standing still once the head turns further than the Body Turn Angle. Turning only goes as far as
/// that angle, so a glance to the side doesn't move the body. Vanilla's own reasons to turn (attacking, aiming,
/// blocking, building) still apply.
/// </summary>
[HarmonyPatch(typeof(Player), "AlwaysRotateCamera")]
internal static class Player_AlwaysRotateCamera_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance, ref bool __result)
    {
        if (__result || __instance != Player.m_localPlayer || !FirstPersonSettings.Active || __instance.IsAttached())
        {
            return;
        }

        if (__instance.m_moveDir.magnitude > 0.1f)
        {
            __result = true;
            return;
        }

        Vector3 look = __instance.GetLookYaw() * Vector3.forward;
        __result = Vector3.Angle(look, __instance.transform.forward) > FirstPersonSettings.TurnAngle;
    }
}
