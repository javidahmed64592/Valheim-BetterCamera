using BetterCamera.Camera;
using BetterCamera.Core;

using HarmonyLib;

using UnityEngine;

namespace BetterCamera.Patches;

/// <summary>
/// Vanilla also checks for walls between the player's eyes and the camera. With the camera orbiting a ship or
/// ship's centre instead, that check would pull the camera in for no reason, so only the pivot's own check is kept.
/// </summary>
[HarmonyPatch(typeof(GameCamera), "CollideRay2")]
[Feature(CameraController.ShipViewFeature)]
internal static class GameCamera_CollideRay2_Patch
{
    [HarmonyPrefix]
    private static void Prefix(ref Vector3 eyePos, Vector3 offsetedEyePos)
    {
        if (CameraController.UsesVehicleView)
        {
            eyePos = offsetedEyePos;
        }
    }
}
