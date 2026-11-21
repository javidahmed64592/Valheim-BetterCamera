using System.Collections.Generic;

using BepInEx.Configuration;

using Jotunn.Configs;
using Jotunn.Managers;

using UnityEngine;

namespace BetterCamera.Camera;

/// <summary>
/// Zoom limits per travel mode, first person, and the views while sailing or riding.
/// <list type="bullet">
/// <item>Walking, sailing (steering a ship) and riding each have their own minimum and maximum zoom, and each
/// remembers its own zoom level, which comes back when returning to that mode.</item>
/// <item>Zooming in past the minimum enters first person at the eyes (vanilla's own first person offset), with the
/// body hidden; zooming out of it returns to the minimum.</item>
/// <item>While sailing or riding, the cycle key switches what the camera orbits between the player (vanilla) and
/// the vehicle's centre. Switching blends over a short time.</item>
/// </list>
/// </summary>
internal static class CameraController
{
    private const string ButtonName = "BetterCamera_CycleView";

    private sealed class ZoomConfig
    {
        public ConfigEntry<float> Min;
        public ConfigEntry<float> Max;
    }

    private static readonly Dictionary<TravelMode, ZoomConfig> Zoom = [];
    private static ConfigEntry<bool> _firstPerson;
    private static ConfigEntry<KeyCode> _cycleKey;
    private static ConfigEntry<float> _shipLift;
    private static ConfigEntry<float> _mountLift;
    private static ConfigEntry<float> _transitionTime;

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

    private static string ButtonQueryName => ButtonName + "!" + BetterCameraPlugin.PluginGUID;

    public static void BindConfig(ConfigFile config)
    {
        Zoom[TravelMode.Walking] = BindZoom(config, "Walking", 1f, 16f, "walking or swimming");
        Zoom[TravelMode.Sailing] = BindZoom(config, "Sailing", 1f, 40f, "steering a ship");
        Zoom[TravelMode.Riding] = BindZoom(config, "Riding", 1f, 24f, "riding a tamed animal");

        _firstPerson = config.Bind("Zoom", "First Person", true,
            "Zooming in past the minimum distance switches to first person, with your body hidden. " +
            "Zooming out again returns to third person.");

        _cycleKey = config.Bind("Keybinds", "Cycle View", KeyCode.C,
            "While steering a ship or riding, switches the camera between following you and following the ship's " +
            "or mount's centre. While doing either, this key no longer toggles walking. " +
            "Rebindable from the in-game controls menu.");
        InputManager.Instance.AddButton(BetterCameraPlugin.PluginGUID, new ButtonConfig { Name = ButtonName, Config = _cycleKey });

        const string views = "Views";
        _shipLift = config.Bind(views, "Ship Height", 4.5f,
            new ConfigDescription("Metres above the waterline of the ship's centre view.", new AcceptableValueRange<float>(0f, 20f)));
        _mountLift = config.Bind(views, "Mount Height", 0.5f,
            new ConfigDescription("Metres above the top of the mount of its centre view.", new AcceptableValueRange<float>(0f, 10f)));
        _transitionTime = config.Bind(views, "Transition Time", 0.4f,
            new ConfigDescription("Seconds the camera takes to move to a new view.", new AcceptableValueRange<float>(0f, 2f)));
    }

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
    /// True while the local player steers a ship or rides, when the cycle key is theirs and C doesn't toggle walking.
    /// </summary>
    internal static bool Travelling(Player player)
    {
        return player != null && ModeOf(player) != TravelMode.Walking;
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
        FirstPersonBody.Restore();
    }

    // ----- Per frame -----

    /// <summary>
    /// The cycle key, from the plugin's Update.
    /// </summary>
    public static void UpdateInput(Player player)
    {
        if (!Travelling(player) || Blocked() || !ZInput.GetButtonDown(ButtonQueryName))
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
            Localization.instance.Localize("$bettercamera_view", Localization.instance.Localize(AnchorToken(mode, next))));
    }

    private static string AnchorToken(TravelMode mode, ViewAnchor anchor)
    {
        string centre = mode == TravelMode.Sailing ? "$bettercamera_view_ship_centre" : "$bettercamera_view_mount_centre";
        return anchor == ViewAnchor.Centre ? centre : "$bettercamera_view_player";
    }

    private static bool Blocked()
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

        // First person only around the player: inside a ship or a mount there's nothing to see.
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

        FirstPersonBody.Update(player, camera.m_distance <= 0f && !player.IsDead());
    }

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
        if (!UsesVehicleView || player != Player.m_localPlayer)
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

    private static Vector3 AnchorPivot(Player player, ViewAnchor anchor, Vector3 playerPivot)
    {
        if (anchor == ViewAnchor.Player)
        {
            return playerPivot;
        }

        Ship ship = player.GetControlledShip();
        if (ship != null && ship.m_floatCollider != null)
        {
            return ShipPivot(ship);
        }

        Character mount = (player.m_doodadController as Sadle)?.GetCharacter();
        return mount != null && mount.GetCollider() != null ? MountPivot(mount) : playerPivot;
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

    /// <summary>
    /// Above the mount's own position, at the height of the top of its collider. The collider isn't always
    /// centred on the body (the lox's sits off to one side), so it only gives the height.
    /// </summary>
    private static Vector3 MountPivot(Character mount)
    {
        Vector3 position = mount.transform.position;
        float top = mount.GetCollider().bounds.max.y;
        return new Vector3(position.x, top + _mountLift.Value, position.z);
    }
}
