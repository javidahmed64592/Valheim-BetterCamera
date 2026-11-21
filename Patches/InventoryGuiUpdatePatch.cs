using BetterCamera.Photo;

using HarmonyLib;

namespace BetterCamera.Patches;

/// <summary>
/// Lets the inventory open and close in photo mode, which vanilla's free fly blocks.
/// </summary>
[HarmonyPatch(typeof(InventoryGui), "Update")]
internal static class InventoryGui_Update_Patch
{
    [HarmonyPostfix]
    private static void Postfix(InventoryGui __instance)
    {
        PhotoMode.UpdateInventory(__instance);
    }
}
