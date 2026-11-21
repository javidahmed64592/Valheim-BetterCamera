using BetterCamera.UI;

using HarmonyLib;

namespace BetterCamera.Patches;

[HarmonyPatch(typeof(Hud), "UpdateShipHud")]
internal static class Hud_UpdateShipHud_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Hud __instance)
    {
        ShipHud.AfterUpdate(__instance);
    }
}
