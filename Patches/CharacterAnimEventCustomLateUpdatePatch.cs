using BetterCamera.Combat;
using BetterCamera.Core;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// Leans the local player's upper body into tilted melee swings, after the animator has posed the bones.
/// </summary>
[HarmonyPatch(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.CustomLateUpdate))]
[Feature(MeleeAim.Feature)]
internal static class CharacterAnimEvent_CustomLateUpdate_Patch
{
    [HarmonyPostfix]
    private static void Postfix(CharacterAnimEvent __instance, float deltaTime)
    {
        if (__instance.m_character is Player player && player == Player.m_localPlayer)
        {
            MeleeLean.Apply(player, deltaTime);
        }
    }
}
