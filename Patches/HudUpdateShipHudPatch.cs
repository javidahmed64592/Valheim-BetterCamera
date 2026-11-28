using BetterCamera.Core;
using BetterCamera.UI;

using HarmonyLib;

namespace BetterCamera.Patches;

[HarmonyPatch(typeof(Hud), "UpdateShipHud")]
[Feature(Feature)]
internal static class Hud_UpdateShipHud_Patch
{
    /// <summary>Without it, the ship's sail and rudder indicator follows the ship as in vanilla, which in first person can be off screen.</summary>
    public const string Feature = "Ship HUD in first person";

    [HarmonyPostfix]
    private static void Postfix(Hud __instance)
    {
        ShipHud.AfterUpdate(__instance);
    }
}
