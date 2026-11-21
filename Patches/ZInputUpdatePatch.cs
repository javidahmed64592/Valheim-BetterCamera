using System.Collections.Generic;

using BetterCamera.Camera;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// While steering a ship or riding, the walk toggle does nothing useful (mounts have their own pace), and its key
/// (C by default) cycles the camera view instead. Straight after the game reads the keys, this cancels the walk
/// toggle's press for the frame, so the player doesn't end up walking after dismounting.
/// </summary>
[HarmonyPatch(typeof(ZInput), nameof(ZInput.Update))]
internal static class ZInput_Update_Patch
{
    private const string ToggleWalk = "ToggleWalk";

    private static readonly AccessTools.FieldRef<ZInput, Dictionary<string, ZInput.ButtonDef>> Buttons =
        AccessTools.FieldRefAccess<ZInput, Dictionary<string, ZInput.ButtonDef>>("m_buttons");

    [HarmonyPostfix]
    private static void Postfix(ZInput __instance)
    {
        if (CameraController.Travelling(Player.m_localPlayer)
            && Buttons(__instance).TryGetValue(ToggleWalk, out ZInput.ButtonDef button)
            && button.Pressed)
        {
            button.ResetState();
        }
    }
}
