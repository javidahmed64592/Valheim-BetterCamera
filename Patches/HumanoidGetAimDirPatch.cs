using BetterCamera.FirstPerson;

using HarmonyLib;

using UnityEngine;

namespace BetterCamera.Patches;

/// <summary>
/// In first person, the local player's attacks aim at what the crosshair is on.
/// </summary>
[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetAimDir))]
internal static class Humanoid_GetAimDir_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Humanoid __instance, Vector3 fromPoint, ref Vector3 __result)
    {
        if (__instance is Player player && player == Player.m_localPlayer && FirstPersonSettings.Active)
        {
            __result = CrosshairAim.DirectionFrom(player, fromPoint, __result);
        }
    }
}
