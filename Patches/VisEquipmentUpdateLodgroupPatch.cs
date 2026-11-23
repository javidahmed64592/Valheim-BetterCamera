using BetterCamera.FirstPerson;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// Vanilla rebuilds a character's LOD group whenever their gear changes, which makes it the place to hear about
/// new or removed gear, so first person can hide the head of anything new.
/// </summary>
[HarmonyPatch(typeof(VisEquipment), "UpdateLodgroup")]
internal static class VisEquipment_UpdateLodgroup_Patch
{
    [HarmonyPostfix]
    private static void Postfix(VisEquipment __instance)
    {
        FirstPersonBody.GearChanged(__instance);
    }
}
