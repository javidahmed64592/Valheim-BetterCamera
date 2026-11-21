using BepInEx.Configuration;

using UnityEngine;

namespace BetterCamera.FirstPerson;

/// <summary>
/// How your gear shows in first person.
/// </summary>
internal enum GearView
{
    /// <summary>
    /// Your whole body except the head, with the game's own animations.
    /// </summary>
    Body = 0,

    /// <summary>
    /// The body hidden and what you hold shown at the bottom of the screen.
    /// </summary>
    HeldItem = 1,
}

/// <summary>
/// First-person settings, and whether the local player is in first person right now.
/// </summary>
internal static class FirstPersonSettings
{
    private static ConfigEntry<GearView> _gearView;
    private static ConfigEntry<float> _turnAngle;
    private static ConfigEntry<Vector3> _cameraOffset;
    private static ConfigEntry<Vector3> _bowAimOffset;

    /// <summary>
    /// The camera's near clip plane in first person, close enough for the hands and gear (vanilla 0.1 to 0.5 m).
    /// </summary>
    public const float NearClip = 0.05f;

    public static GearView Gear => _gearView.Value;

    /// <summary>
    /// How far, in degrees, you can look away from where your body faces before it turns to follow.
    /// </summary>
    public static float TurnAngle => _turnAngle.Value;

    public static Vector3 CameraOffset => _cameraOffset.Value;

    public static Vector3 BowAimOffset => _bowAimOffset.Value;

    public static void BindConfig(ConfigFile config)
    {
        const string section = "First Person";
        _gearView = config.Bind(section, "Gear View", GearView.Body,
            "How your gear shows in first person. Body: your body stays visible apart from the head, so you see " +
            "your arms and gear with the game's own animations. HeldItem: your body is hidden and what you hold is " +
            "shown at the bottom of the screen.");
        _turnAngle = config.Bind(section, "Body Turn Angle", 60f,
            new ConfigDescription("Degrees you can look to the side while standing still before your body turns to " +
                "follow. While moving, your body always faces where you look.",
                new AcceptableValueRange<float>(0f, 180f)));
        _cameraOffset = config.Bind(section, "Camera Offset", new Vector3(0f, 0.1f, 0.12f),
            "Where the first-person camera sits relative to your head's pivot (the top of the neck), in metres " +
            "right, up and forward along where you look. The camera follows the head as the animations move it.");
        _bowAimOffset = config.Bind(section, "Bow Aim Offset", new Vector3(0.15f, 0.03f, 0f),
            "Extra camera offset while drawing a bow, in metres right, up and forward, so the bow (held in front " +
            "of your face) sits beside the crosshair instead of covering it. Arrows still fly to the crosshair.");
    }

    /// <summary>
    /// True while the local player's camera is in first person.
    /// </summary>
    public static bool Active
    {
        get
        {
            GameCamera camera = GameCamera.instance;
            return camera != null && !camera.m_freeFly && camera.m_distance <= 0f && Player.m_localPlayer != null;
        }
    }
}
