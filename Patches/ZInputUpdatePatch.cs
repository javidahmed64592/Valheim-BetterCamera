using System.Collections.Generic;

using BetterCamera.Camera;
using BetterCamera.Core;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// The ship view key (Shift + C by default) includes the walk toggle's key. Straight after the game reads the keys,
/// this cancels the walk toggle's press on the frame the ship view key is used, so switching the view doesn't also
/// toggle walking.
/// </summary>
[HarmonyPatch(typeof(ZInput), nameof(ZInput.Update))]
[Feature(CameraController.ShipViewFeature)]
internal static class ZInput_Update_Patch
{
    private const string ToggleWalk = "ToggleWalk";

    private static readonly AccessTools.FieldRef<ZInput, Dictionary<string, ZInput.ButtonDef>> Buttons =
        AccessTools.FieldRefAccess<ZInput, Dictionary<string, ZInput.ButtonDef>>("m_buttons");

    [HarmonyPostfix]
    private static void Postfix()
    {
        // ZInput.Update is static, so the buttons come from the instance.
        ZInput input = ZInput.instance;
        if (input != null
            && CameraController.CycleKeyDown(Player.m_localPlayer)
            && Buttons(input).TryGetValue(ToggleWalk, out ZInput.ButtonDef button)
            && button.Pressed)
        {
            button.ResetState();
        }
    }
}
