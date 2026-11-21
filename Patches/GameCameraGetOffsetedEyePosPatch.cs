using BetterCamera.Camera;

using HarmonyLib;

using UnityEngine;

namespace BetterCamera.Patches;

/// <summary>
/// Moves the point the camera orbits to the current view's (the ship's or mount's centre, overview or front).
/// </summary>
[HarmonyPatch(typeof(GameCamera), "GetOffsetedEyePos")]
internal static class GameCamera_GetOffsetedEyePos_Patch
{
    [HarmonyPostfix]
    private static void Postfix(ref Vector3 __result)
    {
        Player player = Player.m_localPlayer;
        if (player != null)
        {
            __result = CameraController.Pivot(player, __result);
        }
    }
}
