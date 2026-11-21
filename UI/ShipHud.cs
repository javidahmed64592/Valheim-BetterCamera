using BepInEx.Configuration;

using UnityEngine;

namespace BetterCamera.UI;

/// <summary>
/// The ship's sail and rudder indicator normally floats beside the helm, at the point the ship marks for it. In
/// first person that point is off to the side or behind the camera, so the indicator is pinned to a fixed spot
/// on the screen instead (optionally in third person too).
/// </summary>
internal static class ShipHud
{
    private static ConfigEntry<Vector2> _position;
    private static ConfigEntry<bool> _alwaysFixed;

    public static void BindConfig(ConfigFile config)
    {
        const string section = "Ship Controls";
        _position = config.Bind(section, "Fixed Position", new Vector2(0.5f, 0.25f),
            "Where on the screen the sail and rudder indicator goes in first person, as fractions of the screen's " +
            "width and height from the bottom left (0.5, 0.25 is the middle, a quarter of the way up).");
        _alwaysFixed = config.Bind(section, "Always Fixed", false,
            "Keep the sail and rudder indicator at the fixed position in third person too, instead of beside the helm.");
    }

    /// <summary>
    /// After vanilla's Hud.UpdateShipHud has placed the indicator beside the helm.
    /// </summary>
    public static void AfterUpdate(Hud hud)
    {
        if (hud.m_shipControlsRoot == null || !hud.m_shipHudRoot.activeSelf)
        {
            return;
        }

        GameCamera camera = GameCamera.instance;
        bool firstPerson = camera != null && !camera.m_freeFly && camera.m_distance <= 0f;
        if (!firstPerson && !_alwaysFixed.Value)
        {
            return;
        }

        Vector2 position = _position.Value;
        hud.m_shipControlsRoot.transform.position = new Vector3(
            Screen.width * Mathf.Clamp01(position.x), Screen.height * Mathf.Clamp01(position.y), 0f);
    }
}
