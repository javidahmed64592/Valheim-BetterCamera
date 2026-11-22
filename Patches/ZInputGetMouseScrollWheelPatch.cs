using BetterCamera.Camera;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// Alt+scroll changes the field of view, so vanilla's camera zoom (and anything else reading the scroll wheel)
/// doesn't also get it. The field of view handler reads it itself while this lets it through.
/// </summary>
[HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
internal static class ZInput_GetMouseScrollWheel_Patch
{
    [HarmonyPostfix]
    private static void Postfix(ref float __result)
    {
        if (__result != 0f && CameraController.FovScrollActive(Player.m_localPlayer))
        {
            __result = 0f;
        }
    }
}
