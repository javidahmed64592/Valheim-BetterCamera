using BetterCamera.Core;
using BetterCamera.Photo;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// In photo mode the free fly camera is flown with photo mode's own controls. Vanilla's freefly command on its
/// own still uses vanilla's.
/// </summary>
[HarmonyPatch(typeof(GameCamera), "UpdateFreeFly")]
[Feature(PhotoMode.Feature)]
internal static class GameCamera_UpdateFreeFly_Patch
{
    [HarmonyPrefix]
    private static bool Prefix(GameCamera __instance, float dt)
    {
        if (!PhotoMode.Active)
        {
            return true;
        }

        PhotoMode.UpdateCamera(__instance, dt);
        return false;
    }
}
