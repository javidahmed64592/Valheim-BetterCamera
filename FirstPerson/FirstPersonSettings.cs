using BepInEx.Configuration;

using UnityEngine;

namespace BetterCamera.FirstPerson;

/// <summary>
/// First-person settings, and whether the local player is in first person right now.
/// </summary>
internal static class FirstPersonSettings
{
    private static ConfigEntry<float> _turnAngle;
    private static ConfigEntry<Vector3> _cameraOffset;
    private static ConfigEntry<Vector3> _bowAimOffset;
    private static ConfigEntry<float> _maxLookDown;
    private static ConfigEntry<float> _seatedTurnAngle;
    private static ConfigEntry<float> _seatedMaxLookDown;

    /// <summary>
    /// The camera's near clip plane in first person, close enough for the hands and gear (vanilla 0.1 to 0.5 m).
    /// </summary>
    public const float NearClip = 0.05f;

    /// <summary>
    /// How far, in degrees, you can look away from where your body faces before it turns to follow.
    /// </summary>
    public static float TurnAngle => _turnAngle.Value;

    public static Vector3 CameraOffset => _cameraOffset.Value;

    public static Vector3 BowAimOffset => _bowAimOffset.Value;

    /// <summary>
    /// The furthest you can look down in first person, in degrees below level.
    /// </summary>
    public static float MaxLookDown => _maxLookDown.Value;

    /// <summary>
    /// While seated, how far you can look to either side of where the seat faces, in degrees.
    /// </summary>
    public static float SeatedTurnAngle => _seatedTurnAngle.Value;

    /// <summary>
    /// While seated, the furthest you can look down, in degrees below level.
    /// </summary>
    public static float SeatedMaxLookDown => _seatedMaxLookDown.Value;

    public static void BindConfig(ConfigFile config)
    {
        const string section = "First Person";
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
        _maxLookDown = config.Bind(section, "Max Look Down", 65f,
            new ConfigDescription("The furthest you can look down in first person, in degrees below level, so the " +
                "camera doesn't see inside your armour and body (vanilla 89).",
                new AcceptableValueRange<float>(30f, 89f)));
        _seatedTurnAngle = config.Bind(section, "Seated Turn Angle", 110f,
            new ConfigDescription("While seated (at the helm, on a chair or riding), how far you can look to either " +
                "side, in degrees, so you can look over your shoulders without seeing inside your body.",
                new AcceptableValueRange<float>(30f, 180f)));
        _seatedMaxLookDown = config.Bind(section, "Seated Max Look Down", 60f,
            new ConfigDescription("While seated, the furthest you can look down, in degrees below level.",
                new AcceptableValueRange<float>(30f, 89f)));
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
