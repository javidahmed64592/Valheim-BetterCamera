using BetterCamera.Photo;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// Shows the photo mode key hints in place of vanilla's while photo mode is on.
/// </summary>
[HarmonyPatch(typeof(KeyHints), "UpdateHints")]
internal static class KeyHints_UpdateHints_Patch
{
    [HarmonyPostfix]
    private static void Postfix(KeyHints __instance)
    {
        PhotoKeyHints.AfterUpdateHints(__instance);
    }
}
