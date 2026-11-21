using BepInEx;
using BepInEx.Configuration;

using UnityEngine;

namespace BetterCamera.Core;

internal static class KeyInput
{
    /// <summary>
    /// KeyboardShortcut.IsDown is false while any other key is held, so it would never fire while moving. This
    /// only needs the main key pressed and the modifiers held.
    /// </summary>
    public static bool IsDown(KeyboardShortcut shortcut)
    {
        if (shortcut.MainKey == KeyCode.None || !UnityInput.Current.GetKeyDown(shortcut.MainKey))
        {
            return false;
        }

        foreach (KeyCode modifier in shortcut.Modifiers)
        {
            if (!UnityInput.Current.GetKey(modifier))
            {
                return false;
            }
        }

        return true;
    }
}
