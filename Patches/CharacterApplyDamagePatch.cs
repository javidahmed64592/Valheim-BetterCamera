using BetterCamera.Core;
using BetterCamera.Photo;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// Tells photo mode when the local player really takes damage (hits, burning, poison and so on), so it can end.
/// Vanilla ignores hits of 0.1 or less, so they don't count here either.
/// </summary>
[HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
[Feature(PhotoMode.Feature)]
internal static class Character_ApplyDamage_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Character __instance, HitData hit)
    {
        if (__instance == Player.m_localPlayer && hit.GetTotalDamage() > 0.1f)
        {
            PhotoMode.OnDamaged();
        }
    }
}
