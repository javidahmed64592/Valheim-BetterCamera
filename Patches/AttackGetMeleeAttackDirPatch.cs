using BetterCamera.Combat;

using HarmonyLib;

using UnityEngine;

namespace BetterCamera.Patches;

/// <summary>
/// Tilts the local player's melee swings up or down with their view. Melee hits are worked out on the attacker's
/// client, so this only needs the local player.
/// </summary>
[HarmonyPatch(typeof(Attack), nameof(Attack.GetMeleeAttackDir))]
internal static class Attack_GetMeleeAttackDir_Patch
{
    [HarmonyPostfix]
    private static void Postfix(Attack __instance, Transform originJoint, ref Vector3 attackDir)
    {
        if (__instance.m_character != null && __instance.m_character == Player.m_localPlayer)
        {
            attackDir = MeleeAim.Direction(__instance, originJoint, attackDir);
        }
    }
}
