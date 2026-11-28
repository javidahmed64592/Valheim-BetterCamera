using BetterCamera.Core;
using BetterCamera.Photo;

using HarmonyLib;

using UnityEngine;

namespace BetterCamera.Patches;

/// <summary>
/// Vanilla's free fly also saves a screenshot on right-click. In photo mode only F11 does, so a stray click
/// doesn't fill the screenshots folder.
/// </summary>
[HarmonyPatch(typeof(GameCamera), nameof(GameCamera.ScreenShot))]
[Feature(PhotoMode.Feature)]
internal static class GameCamera_ScreenShot_Patch
{
    [HarmonyPrefix]
    private static bool Prefix()
    {
        return !PhotoMode.Active || ZInput.GetKeyDown(KeyCode.F11);
    }
}
