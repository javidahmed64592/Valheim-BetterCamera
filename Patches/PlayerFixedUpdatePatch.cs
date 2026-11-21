using BetterCamera.FirstPerson;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// Vanilla hides the whole player (body, gear and all) whenever the camera is within 2 m of them, so a close
/// third-person camera doesn't look inside the head. In first person the camera is always that close, so in the
/// body view the player is made visible again; the head itself is hidden by FirstPersonBody.
/// </summary>
[HarmonyPatch(typeof(Player), "FixedUpdate")]
internal static class Player_FixedUpdate_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (__instance == Player.m_localPlayer && FirstPersonSettings.Active && FirstPersonSettings.Gear == GearView.Body)
        {
            __instance.SetVisible(true);
        }
    }
}
