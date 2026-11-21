using BetterCamera.Camera;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// Sets the zoom limits before vanilla zooms, and handles first person after it.
/// </summary>
[HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
internal static class GameCamera_UpdateCamera_Patch
{
    [HarmonyPrefix]
    private static void Prefix(GameCamera __instance)
    {
        CameraController.BeforeUpdate(__instance);
    }

    [HarmonyPostfix]
    private static void Postfix(GameCamera __instance, float dt)
    {
        CameraController.AfterUpdate(__instance, dt);
    }
}
