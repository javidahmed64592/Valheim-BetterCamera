using BepInEx.Configuration;

using UnityEngine;

namespace BetterCamera.FirstPerson;

/// <summary>
/// First-person settings, and whether the local player is in first person right now.
/// </summary>
internal static class FirstPersonSettings
{
    private static ConfigEntry<float> _turnAngle;
    private static ConfigEntry<float> _cameraHeight;
    private static ConfigEntry<float> _cameraForward;
    private static ConfigEntry<float> _headBob;
    private static ConfigEntry<float> _bowAimRight;
    private static ConfigEntry<float> _bowAimUp;
    private static ConfigEntry<float> _headNod;
    private static ConfigEntry<float> _maxLookDown;
    private static ConfigEntry<float> _seatedTurnAngle;
    private static ConfigEntry<float> _seatedMaxLookDown;
    private static ConfigEntry<float> _seatedMaxLookDownSideways;

    /// <summary>
    /// The camera's near clip plane in first person, close enough for the hands and gear (vanilla 0.1 to 0.5 m).
    /// </summary>
    public const float NearClip = 0.05f;

    /// <summary>
    /// How far, in degrees, you can look away from where your body faces before it turns to follow.
    /// </summary>
    public static float TurnAngle => _turnAngle.Value;

    /// <summary>
    /// Where the camera sits from the head's pivot: up and forward along the view (see <see cref="HeadNod"/>).
    /// </summary>
    public static Vector3 CameraOffset => new(0f, _cameraHeight.Value, _cameraForward.Value);

    /// <summary>
    /// How much the camera follows the head's bob, 0 (steady) to 1 (all of it).
    /// </summary>
    public static float HeadBob => _headBob.Value;

    /// <summary>
    /// The extra camera offset while drawing a bow: right and up along the view.
    /// </summary>
    public static Vector3 BowAimOffset => new(_bowAimRight.Value, _bowAimUp.Value, 0f);

    /// <summary>
    /// How much of looking up and down tilts the head (moving the camera offset); the rest is the eyes.
    /// </summary>
    public static float HeadNod => _headNod.Value;

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

    /// <summary>
    /// While seated and looking well to the side, the furthest you can look down, in degrees below level.
    /// </summary>
    public static float SeatedMaxLookDownSideways => _seatedMaxLookDownSideways.Value;

    public static void BindConfig(ConfigFile config)
    {
        const string section = "First Person";
        _turnAngle = config.Bind(section, "Body Turn Angle", 60f,
            new ConfigDescription("Degrees you can look to the side while standing still before your body turns to " +
                "follow. While moving, your body always faces where you look.",
                new AcceptableValueRange<float>(0f, 180f)));
        _cameraHeight = config.Bind(section, "Camera Height", 0.3f,
            new ConfigDescription("How far above your head's pivot (the top of the neck) the first-person camera " +
                "sits, in metres. Raise it if your character feels short. The camera follows the head as the " +
                "animations move it.",
                new AcceptableValueRange<float>(0f, 0.4f)));
        _cameraForward = config.Bind(section, "Camera Forward", 0.08f,
            new ConfigDescription("How far in front of your head's pivot the first-person camera sits, in metres, " +
                "along where you look (see Head Nod for looking up and down).",
                new AcceptableValueRange<float>(-0.1f, 0.3f)));
        _headNod = config.Bind(section, "Head Nod", 0.25f,
            new ConfigDescription("How much your head tilts when you look up or down, moving the camera with it. " +
                "1 tilts the camera's height and forward offset with the view, which drops the camera to your shoulders when you " +
                "look down; 0 keeps it at eye height and only turns the view.",
                new AcceptableValueRange<float>(0f, 1f)));
        _headBob = config.Bind(section, "Head Bob", 0f,
            new ConfigDescription("How much the camera bobs and sways with your head as you walk, run and attack. " +
                "0 (the default) keeps the view steady; 1 follows the head fully.",
                new AcceptableValueRange<float>(0f, 1f)));
        _bowAimRight = config.Bind(section, "Bow Aim Right", 0.15f,
            new ConfigDescription("How far the camera moves right while drawing a bow, in metres, so the bow (held " +
                "in front of your face) sits beside the crosshair instead of covering it. Arrows still fly to the " +
                "crosshair.",
                new AcceptableValueRange<float>(-0.3f, 0.3f)));
        _bowAimUp = config.Bind(section, "Bow Aim Up", 0.03f,
            new ConfigDescription("How far the camera moves up while drawing a bow, in metres.",
                new AcceptableValueRange<float>(-0.2f, 0.2f)));
        _maxLookDown = config.Bind(section, "Max Look Down", 65f,
            new ConfigDescription("The furthest you can look down in first person, in degrees below level, so the " +
                "camera doesn't see inside your armour and body (vanilla 89).",
                new AcceptableValueRange<float>(30f, 89f)));
        _seatedTurnAngle = config.Bind(section, "Seated Turn Angle", 135f,
            new ConfigDescription("While seated (at the helm, on a chair or riding), how far you can look to either " +
                "side, in degrees, so you can look over your shoulders without seeing inside your body.",
                new AcceptableValueRange<float>(30f, 180f)));
        _seatedMaxLookDown = config.Bind(section, "Seated Max Look Down", 60f,
            new ConfigDescription("While seated and facing forward, the furthest you can look down, in degrees " +
                "below level.",
                new AcceptableValueRange<float>(0f, 89f)));
        _seatedMaxLookDownSideways = config.Bind(section, "Seated Max Look Down Sideways", 15f,
            new ConfigDescription("While seated and looking well to the side, the furthest you can look down, in " +
                "degrees below level. Turning while looking down lifts the view towards this, so the camera doesn't " +
                "see inside your body.",
                new AcceptableValueRange<float>(-30f, 89f)));
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
