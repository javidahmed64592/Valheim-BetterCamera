using BetterCamera.FirstPerson;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// Vanilla keeps the near clip plane at 0.5 m unless something is close, which cuts off the hands and the nearest
/// part of held gear in first person. There it uses a fixed, much closer plane.
/// </summary>
[HarmonyPatch(typeof(GameCamera), "UpdateNearClipping")]
internal static class GameCamera_UpdateNearClipping_Patch
{
    [HarmonyPrefix]
    private static bool Prefix(GameCamera __instance)
    {
        if (!FirstPersonSettings.Active || __instance.m_camera == null)
        {
            return true;
        }

        if (__instance.m_camera.nearClipPlane != FirstPersonSettings.NearClip)
        {
            __instance.m_camera.nearClipPlane = FirstPersonSettings.NearClip;
        }

        return false;
    }
}
