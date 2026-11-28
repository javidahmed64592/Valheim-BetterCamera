using BetterCamera.Camera;
using BetterCamera.Core;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// After the game places the chat texts above players' heads: in first person, your own goes to the top of the
/// screen.
/// </summary>
[HarmonyPatch(typeof(Chat), "UpdateWorldTexts")]
[Feature(FirstPersonChat.Feature)]
internal static class Chat_UpdateWorldTexts_Patch
{
    private static void Postfix(Chat __instance)
    {
        FirstPersonChat.Place(__instance);
    }
}
