using System.Collections.Generic;
using System.Globalization;

using BepInEx.Configuration;

using BetterCamera.Combat;
using BetterCamera.Core;
using BetterCamera.FirstPerson;

using Jotunn.Configs;
using Jotunn.Managers;

using UnityEngine;

namespace BetterCamera.Camera;

/// <summary>
/// Zoom limits per travel mode, first person, and the views while sailing or riding.
/// <list type="bullet">
/// <item>Walking, sailing (steering a ship) and riding each have their own minimum and maximum zoom, and each
/// remembers its own zoom level, which comes back when returning to that mode.</item>
/// <item>Zooming in past the minimum enters first person (see FirstPersonBody); zooming out of it returns to the
/// minimum.</item>
/// <item>While steering a ship, the ship view key (Shift + C) switches what the camera orbits between the player
/// (vanilla) and the ship's centre. Switching blends over a short time.</item>
/// <item>The shoulder key moves the third-person camera between the right shoulder (vanilla) and the left, by
/// mirroring vanilla's sideways offset. It blends over the same time.</item>
/// </list>
/// </summary>
internal static class CameraController
{
    private const string ShoulderButtonName = "BetterCamera_SwitchShoulder";
    private const string FirstPersonButtonName = "BetterCamera_FirstPerson";

    private sealed class ZoomConfig
    {
        public ConfigEntry<float> Min;
        public ConfigEntry<float> Max;
    }

    private static readonly Dictionary<TravelMode, ZoomConfig> Zoom = [];
    private static ConfigEntry<bool> _firstPerson;
    private static ConfigEntry<KeyboardShortcut> _cycleKey;
    private static ConfigEntry<KeyCode> _shoulderKey;
    private static ConfigEntry<KeyCode> _firstPersonKey;
    private static ConfigEntry<float> _shipLift;
    private static ConfigEntry<float> _transitionTime;
    private static ConfigEntry<bool> _leftShoulderSaved;
    private static ConfigEntry<float> _thirdPersonFov;
    private static ConfigEntry<float> _firstPersonFov;

    /// <summary>
    /// The field of view the camera has now, easing between the third and first person settings.
    /// </summary>
    private static float _fov;

    /// <summary>
    /// The head's resting place relative to the body for Head Bob, the frame it was last moved on, and whether
    /// it's set.
    /// </summary>
    private static Vector3 _steadyHead;

    private static int _steadyHeadFrame = -1;
    private static bool _hasSteadyHead;

    private static readonly Dictionary<TravelMode, float> Distances = [];
    private static readonly Dictionary<TravelMode, ViewAnchor> Anchors = [];

    private static TravelMode _mode = TravelMode.Walking;
    private static bool _modeKnown;
    private static float _distanceBefore;

    /// <summary>
    /// The view the camera had last frame, to notice when it changes.
    /// </summary>
    private static ViewAnchor _lastAnchor = ViewAnchor.Player;

    /// <summary>
    /// True from zooming into first person until zooming out of it. It carries over when getting on or off a
    /// ship or mount, and back from the centre view (which can't be first person), so the camera only leaves
    /// first person when the player zooms out.
    /// </summary>
    private static bool _inFirstPerson;

    private static ViewAnchor _fromAnchor = ViewAnchor.Player;
    private static float _anchorChangedAt = -100f;

    /// <summary>
    /// True while the third-person camera is over the left shoulder.
    /// </summary>
    private static bool _leftShoulder;

    private static float _shoulderChangedAt = -100f;

    private static string ShoulderButtonQueryName => ShoulderButtonName + "!" + BetterCameraPlugin.PluginGUID;

    private static string FirstPersonButtonQueryName => FirstPersonButtonName + "!" + BetterCameraPlugin.PluginGUID;

    public static void BindConfig(ConfigFile config)
    {
        Zoom[TravelMode.Walking] = BindZoom(config, "Walking", 1f, 16f, "walking or swimming");
        Zoom[TravelMode.Sailing] = BindZoom(config, "Sailing", 1f, 40f, "steering a ship");
        Zoom[TravelMode.Riding] = BindZoom(config, "Riding", 1f, 24f, "riding a tamed animal");

        _firstPerson = config.Bind("Zoom", "First Person", true,
            "Zooming in past the minimum distance switches to first person, seeing your own arms and gear. " +
            "Zooming out again returns to third person.");

        _cycleKey = config.Bind("Keybinds", "Switch Ship View", new KeyboardShortcut(KeyCode.C, KeyCode.LeftShift),
            "While steering a ship, switches the camera between following you and following the ship's centre.");
        _shoulderKey = config.Bind("Keybinds", "Switch Shoulder", KeyCode.K,
            "In third person, moves the camera between your right shoulder (vanilla) and your left. " +
            "Rebindable from the in-game controls menu.");
        InputManager.Instance.AddButton(BetterCameraPlugin.PluginGUID, new ButtonConfig { Name = ShoulderButtonName, Config = _shoulderKey });
        _firstPersonKey = config.Bind("Keybinds", "First Person", KeyCode.L,
            "Switches between first person and third person without zooming all the way in or out. Third person " +
            "comes back at the zoom you last had. Rebindable from the in-game controls menu.");
        InputManager.Instance.AddButton(BetterCameraPlugin.PluginGUID, new ButtonConfig { Name = FirstPersonButtonName, Config = _firstPersonKey });

        const string views = "Views";
        _shipLift = config.Bind(views, "Ship Height", 4.5f,
            new ConfigDescription("Metres above the waterline of the ship's centre view.", new AcceptableValueRange<float>(0f, 20f)));
        _transitionTime = config.Bind(views, "Transition Time", 0.4f,
            new ConfigDescription("Seconds the camera takes to move to a new view.", new AcceptableValueRange<float>(0f, 2f)));
        _leftShoulderSaved = config.Bind(views, "Left Shoulder", false,
            "Whether the third-person camera is over your left shoulder. The shoulder key changes it, and it's " +
            "remembered between games.");
        _leftShoulder = _leftShoulderSaved.Value;

        const string fov = "Field of View";
        _thirdPersonFov = config.Bind(fov, "Third Person", 65f,
            new ConfigDescription("The camera's field of view in third person, in degrees (vanilla 65). Alt + " +
                "scroll changes the one you're in while playing.",
                new AcceptableValueRange<float>(MinFov, MaxFov)));
        _firstPersonFov = config.Bind(fov, "First Person", 65f,
            new ConfigDescription("The camera's field of view in first person, in degrees (vanilla 65). A wider " +
                "view can feel less cramped.",
                new AcceptableValueRange<float>(MinFov, MaxFov)));
    }

    /// <summary>
    /// The field of view while playing, which photo mode starts from and resets to.
    /// </summary>
    public static float GameplayFov => _fov > 0f ? _fov : _thirdPersonFov.Value;

    private static ZoomConfig BindZoom(ConfigFile config, string mode, float min, float max, string what)
    {
        string section = "Zoom " + mode;
        return new ZoomConfig
        {
            Min = config.Bind(section, "Min Distance", min,
                new ConfigDescription($"Closest the third-person camera gets while {what}, in metres (vanilla 1).",
                    new AcceptableValueRange<float>(0.5f, 10f))),
            Max = config.Bind(section, "Max Distance", max,
                new ConfigDescription($"Furthest the camera zooms out while {what}, in metres.",
                    new AcceptableValueRange<float>(2f, 100f))),
        };
    }

    private static float Min(TravelMode mode)
    {
        return Zoom[mode].Min.Value;
    }

    private static float Max(TravelMode mode)
    {
        return Mathf.Max(Zoom[mode].Max.Value, Min(mode));
    }

    private static TravelMode ModeOf(Player player)
    {
        return (player.GetControlledShip() != null, player.IsRiding()) switch
        {
            (true, _) => TravelMode.Sailing,
            (_, true) => TravelMode.Riding,
            _ => TravelMode.Walking,
        };
    }

    /// <summary>
    /// True while the local player steers a ship, when the ship view key is theirs.
    /// </summary>
    internal static bool Steering(Player player)
    {
        return player != null && ModeOf(player) == TravelMode.Sailing;
    }

    /// <summary>
    /// The view of the current mode; always the player while walking.
    /// </summary>
    private static ViewAnchor Anchor
    {
        get
        {
            bool chosen = Anchors.TryGetValue(_mode, out ViewAnchor anchor);
            return _mode != TravelMode.Walking && chosen ? anchor : ViewAnchor.Player;
        }
    }

    public static void Reset()
    {
        Distances.Clear();
        Anchors.Clear();
        _modeKnown = false;
        _mode = TravelMode.Walking;
        _lastAnchor = ViewAnchor.Player;
        _inFirstPerson = false;
        _anchorChangedAt = -100f;
        _shoulderChangedAt = -100f;
        FirstPersonBody.Restore();
    }

    // ----- Per frame -----

    /// <summary>
    /// The ship view key, from the plugin's Update.
    /// </summary>
    public static void UpdateInput(Player player)
    {
        UpdateShoulderInput(player);
        UpdateFirstPersonInput();
        UpdateFovInput(player);
        if (!CycleKeyDown(player))
        {
            return;
        }

        TravelMode mode = ModeOf(player);
        ViewAnchor current = Anchors.TryGetValue(mode, out ViewAnchor anchor) ? anchor : ViewAnchor.Player;
        ViewAnchor next = current == ViewAnchor.Player ? ViewAnchor.Centre : ViewAnchor.Player;
        _fromAnchor = current;
        _anchorChangedAt = Time.time;
        Anchors[mode] = next;
        player.Message(MessageHud.MessageType.TopLeft,
            Localization.instance.Localize("$bettercamera_view", Localization.instance.Localize(AnchorToken(next))));
    }

    /// <summary>
    /// Degrees each Alt+scroll notch changes the field of view by.
    /// </summary>
    private const float FovStep = 5f;

    /// <summary>
    /// True while reading the scroll wheel for the field of view, so the scroll patch lets it through.
    /// </summary>
    private static bool _readingFovScroll;

    /// <summary>
    /// Alt+scroll changes the field of view, and vanilla's zoom doesn't also get the scroll. Only without a build
    /// tool out (the hammer and hoe use Alt+scroll themselves, in BetterHammer and BetterHoe) and outside photo
    /// mode (which uses it for speed).
    /// </summary>
    internal static bool FovScrollActive(Player player)
    {
        return !_readingFovScroll
            && player != null
            && (ZInput.GetKey(KeyCode.LeftAlt) || ZInput.GetKey(KeyCode.RightAlt))
            && !player.InPlaceMode()
            && !Photo.PhotoMode.Active
            && !Blocked();
    }

    /// <summary>
    /// Alt+scroll: changes the field of view of the view you're in (first or third person) and saves it.
    /// </summary>
    private static void UpdateFovInput(Player player)
    {
        if (!FovScrollActive(player))
        {
            return;
        }

        _readingFovScroll = true;
        float scroll = ZInput.GetMouseScrollWheel();
        _readingFovScroll = false;
        if (scroll == 0f)
        {
            return;
        }

        ConfigEntry<float> setting = FirstPersonSettings.Active ? _firstPersonFov : _thirdPersonFov;
        float step = scroll > 0f ? -FovStep : FovStep;
        setting.Value = Mathf.Clamp(Mathf.Round((setting.Value + step) / FovStep) * FovStep, MinFov, MaxFov);
        string text = Localization.instance.Localize("$bettercamera_fov", setting.Value.ToString("0", CultureInfo.CurrentCulture));
        MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, text, log: false);
    }

    private const float MinFov = 30f;
    private const float MaxFov = 120f;

    /// <summary>
    /// The shoulder key, in third person only: in first person there's no shoulder to look over.
    /// </summary>
    /// <summary>
    /// The first person key: jumps into first person, or back out to the mode's last third-person zoom. Only
    /// where first person is allowed (around the player, not the ship's centre view).
    /// </summary>
    private static void UpdateFirstPersonInput()
    {
        GameCamera camera = GameCamera.instance;
        if (camera == null || camera.m_freeFly || !_firstPerson.Value || Anchor != ViewAnchor.Player || !_modeKnown
            || Blocked() || !ZInput.GetButtonDown(FirstPersonButtonQueryName))
        {
            return;
        }

        if (camera.m_distance <= 0f)
        {
            camera.m_distance = ThirdPersonDistance(_mode, Min(_mode));
        }
        else
        {
            Distances[_mode] = camera.m_distance;
            camera.m_distance = 0f;
        }
    }

    private static void UpdateShoulderInput(Player player)
    {
        GameCamera camera = GameCamera.instance;
        if (camera == null || camera.m_freeFly || camera.m_distance <= 0f || Blocked()
            || !ZInput.GetButtonDown(ShoulderButtonQueryName))
        {
            return;
        }

        _leftShoulder = !_leftShoulder;
        _leftShoulderSaved.Value = _leftShoulder;
        _shoulderChangedAt = Time.time;
        string token = _leftShoulder ? "$bettercamera_view_left_shoulder" : "$bettercamera_view_right_shoulder";
        player.Message(MessageHud.MessageType.TopLeft,
            Localization.instance.Localize("$bettercamera_view", Localization.instance.Localize(token)));
    }

    /// <summary>
    /// True on the frame the ship view key is pressed while steering.
    /// </summary>
    internal static bool CycleKeyDown(Player player)
    {
        return Steering(player) && !Blocked() && KeyInput.IsDown(_cycleKey.Value);
    }

    private static string AnchorToken(ViewAnchor anchor)
    {
        return anchor == ViewAnchor.Centre ? "$bettercamera_view_ship_centre" : "$bettercamera_view_player";
    }

    internal static bool Blocked()
    {
        return Menu.IsVisible()
            || InventoryGui.IsVisible()
            || Minimap.IsOpen()
            || (Chat.instance != null && Chat.instance.HasFocus())
            || Console.IsVisible()
            || TextInput.IsVisible();
    }

    /// <summary>
    /// Before vanilla's GameCamera.UpdateCamera: picks the mode, swaps in its zoom level, and sets the limits
    /// vanilla clamps the zoom to.
    /// </summary>
    public static void BeforeUpdate(GameCamera camera)
    {
        Player player = Player.m_localPlayer;
        if (player == null || camera.m_freeFly)
        {
            return;
        }

        TravelMode mode = ModeOf(player);
        bool modeChanged = !_modeKnown || mode != _mode;
        if (modeChanged)
        {
            // Each mode remembers its third-person zoom; first person is carried over separately.
            if (_modeKnown && camera.m_distance > 0f)
            {
                Distances[_mode] = camera.m_distance;
            }

            _mode = mode;
            _modeKnown = true;
            _anchorChangedAt = -100f;
        }

        // First person only around the player: inside a ship there's nothing to see.
        ViewAnchor anchor = Anchor;
        bool firstPersonAllowed = _firstPerson.Value && anchor == ViewAnchor.Player;
        if (modeChanged || anchor != _lastAnchor)
        {
            // Switching view within a mode: keep its third-person zoom from either view.
            if (!modeChanged && camera.m_distance > 0f)
            {
                Distances[mode] = camera.m_distance;
            }

            camera.m_distance = firstPersonAllowed && _inFirstPerson ? 0f : ThirdPersonDistance(mode, camera.m_distance);
            _lastAnchor = anchor;
        }

        camera.m_minDistance = firstPersonAllowed ? 0f : Min(mode);
        camera.m_maxDistance = Max(mode);
        camera.m_maxDistanceBoat = Max(mode);
        if (!firstPersonAllowed && camera.m_distance < Min(mode))
        {
            camera.m_distance = Min(mode);
        }

        _distanceBefore = camera.m_distance;
    }

    /// <summary>
    /// The mode's remembered third-person zoom, or the current one within its limits.
    /// </summary>
    private static float ThirdPersonDistance(TravelMode mode, float current)
    {
        return Distances.TryGetValue(mode, out float distance) ? distance : Mathf.Clamp(current, Min(mode), Max(mode));
    }

    /// <summary>
    /// After vanilla's GameCamera.UpdateCamera: between first person and the minimum distance there's nothing
    /// worth seeing (the camera would be inside the head), so a zoom that lands there jumps to first person when
    /// zooming in, or back to the minimum when zooming out, and the camera is placed again.
    /// </summary>
    public static void AfterUpdate(GameCamera camera, float dt)
    {
        Player player = Player.m_localPlayer;
        if (player == null || camera.m_freeFly)
        {
            FirstPersonBody.Restore();
            return;
        }

        float distance = camera.m_distance;
        float min = Min(_mode);
        if (distance > 0f && distance < min)
        {
            camera.m_distance = distance < _distanceBefore ? 0f : min;
            if (!player.IsDead() && !(player.IsAttached() && player.GetAttachCameraPoint() != null))
            {
                camera.GetCameraPosition(dt, out Vector3 position, out Quaternion rotation);
                camera.transform.SetPositionAndRotation(position, rotation);
            }
        }

        // Only zooming changes this: around the player, first person is exactly zoom 0.
        if (Anchor == ViewAnchor.Player)
        {
            _inFirstPerson = camera.m_distance <= 0f;
        }

        bool firstPerson = camera.m_distance <= 0f && !player.IsDead();
        FirstPersonBody.Update(player, firstPerson);
        if (!firstPerson)
        {
            _hasSteadyHead = false;
        }

        ApplyFov(camera, FirstPersonSettings.Active ? _firstPersonFov.Value : _thirdPersonFov.Value);
    }

    /// <summary>
    /// Sets the field of view after vanilla sets its own, easing between the third and first person values.
    /// </summary>
    private static void ApplyFov(GameCamera camera, float target)
    {
        _fov = _fov <= 0f ? target : Mathf.Lerp(_fov, target, 1f - Mathf.Exp(-FovEaseRate * Time.unscaledDeltaTime));
        camera.m_camera.fieldOfView = _fov;
        camera.m_skyCamera.fieldOfView = _fov;
    }

    /// <summary>
    /// How quickly the field of view eases to a new value, per second.
    /// </summary>
    private const float FovEaseRate = 10f;

    /// <summary>
    /// How quickly the steady head follows the real one for Head Bob, per second: slow enough to smooth out the
    /// bob of walking and running, fast enough to follow crouching and sitting.
    /// </summary>
    private const float SteadyHeadRate = 4f;

    // ----- Views -----

    /// <summary>
    /// True while the camera orbits something other than the player, including while blending to or from it.
    /// </summary>
    internal static bool UsesVehicleView => _mode != TravelMode.Walking && (Anchor != ViewAnchor.Player || Blending);

    private static bool Blending => Time.time - _anchorChangedAt < _transitionTime.Value;

    /// <summary>
    /// Replaces vanilla's pivot (`playerPivot`, around the player's head) with the current view's.
    /// </summary>
    public static Vector3 Pivot(Player player, Vector3 playerPivot)
    {
        if (player == Player.m_localPlayer && Anchor == ViewAnchor.Player && FirstPersonSettings.Active
            && player.m_head != null && !player.IsDead())
        {
            return FirstPersonPivot(player);
        }

        if (player != Player.m_localPlayer)
        {
            return playerPivot;
        }

        playerPivot += ShoulderShift(player);
        if (!UsesVehicleView)
        {
            return playerPivot;
        }

        Vector3 to = AnchorPivot(player, Anchor, playerPivot);
        if (!Blending || _transitionTime.Value <= 0f)
        {
            return to;
        }

        Vector3 from = AnchorPivot(player, _fromAnchor, playerPivot);
        float t = Mathf.SmoothStep(0f, 1f, (Time.time - _anchorChangedAt) / _transitionTime.Value);
        return Vector3.Lerp(from, to, t);
    }

    /// <summary>
    /// In first person the camera rides on the head bone, so it follows every lean and bob of the animations
    /// instead of the body moving in front of it. The offset turns with the view, like eyes on a neck. Head Bob
    /// below 1 blends towards a steadied head (its place on the body, smoothed), for a calmer view.
    /// </summary>
    private static Vector3 FirstPersonPivot(Player player)
    {
        // Ease the bow offset in and out over about 0.15 s.
        float target = player.IsDrawingBow() ? 1f : 0f;
        _bowAim = Mathf.MoveTowards(_bowAim, target, Time.deltaTime / 0.15f);
        Vector3 offset = FirstPersonSettings.CameraOffset + (FirstPersonSettings.BowAimOffset * Mathf.SmoothStep(0f, 1f, _bowAim));
        return SteadiedHead(player) + (player.m_eye.rotation * offset);
    }

    /// <summary>
    /// Moves vanilla's third-person pivot from the right shoulder towards the left: vanilla adds its offset (0.3 m
    /// right in the default camera) along the eye's axes, so this adds twice its sideways part the other way, blended
    /// while switching.
    /// </summary>
    private static Vector3 ShoulderShift(Player player)
    {
        GameCamera camera = GameCamera.instance;
        if (camera == null || camera.m_distance <= 0f || player.InBed() || player.m_eye == null)
        {
            return Vector3.zero;
        }

        float t = _transitionTime.Value <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, (Time.time - _shoulderChangedAt) / _transitionTime.Value);
        float left = _leftShoulder ? t : 1f - t;
        if (left <= 0f)
        {
            return Vector3.zero;
        }

        Vector3 offset = player.UseMeleeCamera() ? camera.m_3rdCombatOffset : camera.m_3rdOffset;
        return player.m_eye.TransformVector(Vector3.left * (2f * offset.x * left));
    }

    /// <summary>
    /// The head's place with Head Bob applied. The smoothing steps once a frame, as the camera can be placed more
    /// than once a frame.
    /// </summary>
    private static Vector3 SteadiedHead(Player player)
    {
        Transform body = player.transform;
        // The unbent head, so leaning into a melee swing doesn't move the view.
        Vector3 head = body.InverseTransformPoint(MeleeLean.HeadPosition(player));
        if (!_hasSteadyHead)
        {
            _steadyHead = head;
            _hasSteadyHead = true;
        }
        else if (_steadyHeadFrame != Time.frameCount)
        {
            _steadyHead = Vector3.Lerp(_steadyHead, head, 1f - Mathf.Exp(-SteadyHeadRate * Time.deltaTime));
        }

        _steadyHeadFrame = Time.frameCount;
        return body.TransformPoint(Vector3.Lerp(_steadyHead, head, FirstPersonSettings.HeadBob));
    }

    /// <summary>
    /// How far the camera has moved to the bow aiming position, 0 to 1.
    /// </summary>
    private static float _bowAim;

    private static Vector3 AnchorPivot(Player player, ViewAnchor anchor, Vector3 playerPivot)
    {
        if (anchor == ViewAnchor.Player)
        {
            return playerPivot;
        }

        Ship ship = player.GetControlledShip();
        return ship != null && ship.m_floatCollider != null ? ShipPivot(ship) : playerPivot;
    }

    /// <summary>
    /// The ship's float collider marks its hull at the waterline. The height is in world up, so the view doesn't
    /// rock with the waves.
    /// </summary>
    private static Vector3 ShipPivot(Ship ship)
    {
        BoxCollider hull = ship.m_floatCollider;
        return hull.transform.TransformPoint(hull.center) + (Vector3.up * _shipLift.Value);
    }

}
