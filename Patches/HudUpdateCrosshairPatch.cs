using BetterCamera.Photo;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// In photo mode the character can't interact with anything, so what the camera looks at gets no hover text,
/// yellow crosshair or piece health bar.
/// </summary>
[HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
internal static class Hud_UpdateCrosshair_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Hud __instance)
    {
        if (!PhotoMode.Active)
        {
            return;
        }

        __instance.m_hoverName.text = "";
        __instance.m_crosshair.color = Hud.s_whiteHalfAlpha;
        __instance.m_pieceHealthRoot.gameObject.SetActive(false);
    }
}
