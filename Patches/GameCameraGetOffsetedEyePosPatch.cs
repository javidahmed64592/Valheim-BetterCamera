using BetterCamera.Camera;
using BetterCamera.Core;

using HarmonyLib;

using UnityEngine;

namespace BetterCamera.Patches;

/// <summary>
/// Moves the point the camera orbits to the ship's centre while that view is on.
/// </summary>
[HarmonyPatch(typeof(GameCamera), "GetOffsetedEyePos")]
[Feature(CameraController.ShipViewFeature)]
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
