using BetterCamera.FirstPerson;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// In first person, looking down is limited to Max Look Down (vanilla allows 89 degrees), so the camera doesn't
/// see inside the player's armour and body. Looking up is unchanged.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.SetMouseLook))]
internal static class Player_SetMouseLook_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        // Positive pitch looks down.
        if (__instance != Player.m_localPlayer || !FirstPersonSettings.Active
            || __instance.m_lookPitch <= FirstPersonSettings.MaxLookDown)
        {
            return;
        }

        __instance.m_lookPitch = FirstPersonSettings.MaxLookDown;
        __instance.UpdateEyeRotation();
        __instance.m_lookDir = __instance.m_eye.forward;
    }
}
