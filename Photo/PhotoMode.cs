using System.Globalization;

using BepInEx.Configuration;

using BetterCamera.Camera;

using Jotunn.Configs;
using Jotunn.Managers;

using UnityEngine;

namespace BetterCamera.Photo;

/// <summary>
/// A free camera for taking pictures, built on vanilla's free fly camera (the `freefly` console command).
/// <list type="bullet">
/// <item>While it's on, vanilla stops the character taking input, so it stands still, and grass and nearby detail
/// follow the camera. The world keeps going, and taking damage (Character.ApplyDamage) ends photo mode.</item>
/// <item>The camera is flown with this class's own controls instead of vanilla's, so they match the key hints:
/// move, up and down, faster, zoom, speed, roll, look at a point, and follow something that moves.</item>
/// <item>It stays within Max Distance of the character.</item>
/// <item>The hotbar, map and health are always hidden (PhotoHud); the hide UI key hides the rest, through
/// vanilla's own HUD hiding.</item>
/// <item>The inventory still opens, so gear can be changed while posing.</item>
/// <item>Screenshots are left to the player (vanilla F11 or their own tool).</item>
/// </list>
/// </summary>
internal static class PhotoMode
{
    private const string ButtonName = "BetterCamera_PhotoMode";

    private const float FovStep = 1.1f;
    private const float SpeedStep = 1.2f;
    private const float MinSpeed = 0.25f;
    private const float MaxSpeed = 100f;
    private const float RollSpeed = 45f;
    private const float MaxPitch = 89f;

    private static ConfigEntry<KeyCode> _toggleKey;
    private static ConfigEntry<float> _maxDistance;
    private static ConfigEntry<float> _startSpeed;
    private static ConfigEntry<float> _fastMultiplier;
    private static ConfigEntry<float> _smoothing;
    private static ConfigEntry<bool> _exitOnDamage;
    private static ConfigEntry<KeyCode> _rollLeftKey;
    private static ConfigEntry<KeyCode> _rollRightKey;
    private static ConfigEntry<KeyCode> _resetKey;
    private static ConfigEntry<KeyCode> _hideUiKey;

    private static bool _active;
    private static float _yaw;
    private static float _pitch;
    private static float _roll;
    private static float _fov;
    private static float _speed;
    private static Vector3 _velocity;
    private static Vector3 _acceleration;
    private static Quaternion _rotationVelocity = Quaternion.identity;
    private static bool _damaged;
    private static bool _hudWasHidden;

    /// <summary>
    /// What the camera keeps looking at, with the point on it in its own space.
    /// </summary>
    private static Transform _lookAt;

    private static Vector3 _lookAtPoint;

    /// <summary>
    /// What the camera moves along with, and the camera's offset from it in world axes. The offset doesn't turn
    /// with it: a creature or ship turning would otherwise swing the camera round it.
    /// </summary>
    private static Transform _follow;

    private static Vector3 _followOffset;

    private static string ButtonQueryName => ButtonName + "!" + BetterCameraPlugin.PluginGUID;

    /// <summary>
    /// True while photo mode flies the camera.
    /// </summary>
    public static bool Active
    {
        get
        {
            // Vanilla's freefly command can turn free fly off underneath photo mode.
            if (_active && (GameCamera.instance == null || !GameCamera.instance.m_freeFly))
            {
                _active = false;
            }

            return _active;
        }
    }

    public static KeyCode ToggleKey => _toggleKey.Value;

    public static KeyCode RollLeftKey => _rollLeftKey.Value;

    public static KeyCode RollRightKey => _rollRightKey.Value;

    public static KeyCode ResetKey => _resetKey.Value;

    public static KeyCode HideUiKey => _hideUiKey.Value;

    public static bool LookingAt => _lookAt != null;

    public static bool Following => _follow != null;

    public static void BindConfig(ConfigFile config)
    {
        _toggleKey = config.Bind("Keybinds", "Photo Mode", KeyCode.J,
            "Turns photo mode on and off: a free camera for taking pictures while your character stands still. " +
            "Rebindable from the in-game controls menu.");
        InputManager.Instance.AddButton(BetterCameraPlugin.PluginGUID, new ButtonConfig { Name = ButtonName, Config = _toggleKey });
        _rollLeftKey = config.Bind("Keybinds", "Photo Mode Roll Left", KeyCode.Q, "In photo mode, tilts the camera to the left.");
        _rollRightKey = config.Bind("Keybinds", "Photo Mode Roll Right", KeyCode.E, "In photo mode, tilts the camera to the right.");
        _resetKey = config.Bind("Keybinds", "Photo Mode Reset", KeyCode.R, "In photo mode, levels the camera and resets the zoom.");
        _hideUiKey = config.Bind("Keybinds", "Photo Mode Hide UI", KeyCode.U,
            "In photo mode, hides or shows what's left of the UI (key hints, status effects, crosshair and " +
            "messages). The hotbar, map and health are always hidden in photo mode.");

        const string section = "Photo Mode";
        _maxDistance = config.Bind(section, "Max Distance", 100f,
            new ConfigDescription("How far the photo mode camera can fly from your character, in metres.",
                new AcceptableValueRange<float>(5f, 1000f)));
        _startSpeed = config.Bind(section, "Speed", 5f,
            new ConfigDescription("How fast the camera flies when photo mode starts, in metres per second. Alt + " +
                "scroll changes it while flying.",
                new AcceptableValueRange<float>(MinSpeed, MaxSpeed)));
        _fastMultiplier = config.Bind(section, "Fast Multiplier", 4f,
            new ConfigDescription("How much faster the camera flies while holding the run key.",
                new AcceptableValueRange<float>(1f, 20f)));
        _smoothing = config.Bind(section, "Smoothing", 0f,
            new ConfigDescription("Seconds the camera takes to catch up with your movement and turning, for smooth " +
                "shots. 0 follows your input exactly.",
                new AcceptableValueRange<float>(0f, 2f)));
        _exitOnDamage = config.Bind(section, "Exit On Damage", true,
            "Leaves photo mode when your character takes damage, so you can defend yourself.");
    }

    /// <summary>
    /// The toggle key and leaving on damage, from the plugin's Update.
    /// </summary>
    public static void UpdateInput(Player player)
    {
        if (Active && (player.IsDead() || (_exitOnDamage.Value && _damaged)))
        {
            Exit(player, "$bettercamera_photo_damage");
            return;
        }

        if (CameraController.Blocked())
        {
            return;
        }

        // Jotunn only reports its buttons while the player takes input, which vanilla stops in free fly, so in
        // photo mode the keys are read directly.
        if (Active)
        {
            if (ZInput.GetKeyDown(_toggleKey.Value))
            {
                Exit(player, "$bettercamera_photo_off");
            }
            else if (ZInput.GetKeyDown(_hideUiKey.Value) && Hud.instance != null)
            {
                Hud.instance.m_userHidden = !Hud.instance.m_userHidden;
            }
        }
        else if (ZInput.GetButtonDown(ButtonQueryName))
        {
            Enter(player);
        }
    }

    private static void Enter(Player player)
    {
        GameCamera camera = GameCamera.instance;
        if (camera == null || camera.m_freeFly || player.IsDead())
        {
            return;
        }

        // Start from wherever the camera is now, looking the same way.
        Vector3 angles = camera.transform.eulerAngles;
        _yaw = angles.y;
        _pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, angles.x), -MaxPitch, MaxPitch);
        _roll = 0f;
        _fov = CameraController.GameplayFov;
        _speed = _startSpeed.Value;
        _velocity = Vector3.zero;
        _acceleration = Vector3.zero;
        _rotationVelocity = Quaternion.identity;
        _lookAt = null;
        _follow = null;
        _damaged = false;
        _hudWasHidden = Hud.instance != null && Hud.instance.m_userHidden;

        camera.m_freeFly = true;
        _active = true;
        player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$bettercamera_photo_on"));
    }

    /// <summary>
    /// The local player took damage. Health itself isn't watched, as it also falls without any damage, such as when
    /// food wears off and lowers the maximum.
    /// </summary>
    public static void OnDamaged()
    {
        _damaged |= Active;
    }

    /// <summary>
    /// Back to the normal camera, with the HUD shown or hidden as it was before photo mode.
    /// </summary>
    public static void Exit(Player player, string messageToken)
    {
        if (!_active)
        {
            return;
        }

        _active = false;
        _lookAt = null;
        _follow = null;
        GameCamera camera = GameCamera.instance;
        if (camera != null)
        {
            camera.m_freeFly = false;
        }

        PhotoHud.Restore();
        if (Hud.instance != null)
        {
            Hud.instance.m_userHidden = _hudWasHidden;
        }

        if (player != null && messageToken != null)
        {
            player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize(messageToken));
        }
    }

    /// <summary>
    /// Opens and closes the inventory in photo mode, so you can change gear while posing. Vanilla skips all its
    /// inventory keys in free fly; this does what it would do otherwise, from the end of InventoryGui.Update, so it
    /// happens at the same point (closing with Escape doesn't also open the menu).
    /// </summary>
    public static void UpdateInventory(InventoryGui gui)
    {
        Player player = Player.m_localPlayer;
        if (!Active || player == null || player.IsDead()
            || (Chat.instance != null && Chat.instance.HasFocus()) || Console.IsVisible() || Menu.IsVisible()
            || Minimap.IsOpen() || TextInput.IsVisible())
        {
            return;
        }

        if (InventoryGui.IsVisible())
        {
            bool close = ZInput.GetButtonDown("Inventory") || ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("Use");
            if (gui.m_shownFrames > 1 && close)
            {
                ZInput.ResetButtonStatus("Inventory");
                ZInput.ResetButtonStatus("Use");
                gui.Hide();
            }
        }
        else if (gui.m_hiddenFrames > 1 && ZInput.GetButtonDown("Inventory"))
        {
            ZInput.ResetButtonStatus("Inventory");
            gui.Show(null);
        }
    }

    /// <summary>
    /// Flies the camera, in place of vanilla's UpdateFreeFly.
    /// </summary>
    public static void UpdateCamera(GameCamera camera, float dt)
    {
        Transform transform = camera.transform;
        Vector3 move = Vector3.zero;
        float fast = 1f;
        if (!CameraController.Blocked())
        {
            move = ReadInput(camera, dt, out bool running);
            fast = running ? _fastMultiplier.Value : 1f;
        }

        PhotoHud.Hide();

        // Moving goes where the camera faces (also while it looks at something), without the roll.
        Quaternion look = Quaternion.Euler(0f, _yaw, 0f) * Quaternion.Euler(_pitch, 0f, 0f);
        Quaternion facing = transform.rotation * Quaternion.Euler(0f, 0f, -_roll);
        Vector3 target = facing * Vector3.ClampMagnitude(move, 1f) * (_speed * fast);
        _velocity = _smoothing.Value <= 0f
            ? target
            : Vector3.SmoothDamp(_velocity, target, ref _acceleration, _smoothing.Value, Mathf.Infinity, dt);

        Vector3 position = transform.position;
        if (_follow != null)
        {
            _followOffset += _velocity * dt;
            position = _follow.position + _followOffset;
        }
        else
        {
            // What was followed may have been destroyed, which Unity counts as null: let it go.
            _follow = null;
            position += _velocity * dt;
        }

        position = KeepNearPlayer(position);
        if (_follow != null)
        {
            _followOffset = position - _follow.position;
        }

        transform.position = position;

        Quaternion rotation = look;
        if (_lookAt != null)
        {
            Vector3 towards = _lookAt.TransformPoint(_lookAtPoint) - position;
            if (towards.sqrMagnitude > 0.0001f)
            {
                rotation = Quaternion.LookRotation(towards, Vector3.up);
            }
        }
        else
        {
            // Likewise for a destroyed look-at target.
            _lookAt = null;
        }

        rotation *= Quaternion.Euler(0f, 0f, _roll);
        transform.rotation = _smoothing.Value <= 0f
            ? rotation
            : Utils.SmoothDamp(transform.rotation, rotation, ref _rotationVelocity, _smoothing.Value, 9999f, dt);

        camera.m_camera.fieldOfView = _fov;
        camera.m_skyCamera.fieldOfView = _fov;
    }

    /// <summary>
    /// Turning, zoom, speed, roll and the click targets. Returns the movement keys as a direction.
    /// </summary>
    private static Vector3 ReadInput(GameCamera camera, float dt, out bool running)
    {
        Vector2 mouse = ZInput.GetMouseDelta() * PlayerController.m_mouseSens;
        if (PlayerController.m_invertMouse)
        {
            mouse.y *= -1f;
        }

        if (_lookAt == null)
        {
            _yaw += mouse.x;
            _pitch = Mathf.Clamp(_pitch - mouse.y, -MaxPitch, MaxPitch);
        }

        // Scroll zooms; with Alt it changes the speed.
        float scroll = ZInput.GetMouseScrollWheel();
        if (scroll != 0f)
        {
            bool alt = ZInput.GetKey(KeyCode.LeftAlt) || ZInput.GetKey(KeyCode.RightAlt);
            float step = scroll > 0f ? 1f : -1f;
            if (alt)
            {
                _speed = Mathf.Clamp(_speed * Mathf.Pow(SpeedStep, step), MinSpeed, MaxSpeed);
                ShowSpeed();
            }
            else
            {
                _fov = Mathf.Clamp(_fov / Mathf.Pow(FovStep, step), camera.m_freeFlyMinFov, camera.m_freeFlyMaxFov);
            }
        }

        if (ZInput.GetKey(_rollLeftKey.Value))
        {
            _roll += RollSpeed * dt;
        }

        if (ZInput.GetKey(_rollRightKey.Value))
        {
            _roll -= RollSpeed * dt;
        }

        if (ZInput.GetKeyDown(_resetKey.Value))
        {
            _roll = 0f;
            _fov = CameraController.GameplayFov;
        }

        if (ZInput.GetButtonDown("Attack"))
        {
            ToggleLookAt(camera.transform);
        }

        if (ZInput.GetButtonDown("SecondaryAttack"))
        {
            ToggleFollow(camera.transform);
        }

        Vector3 move = Vector3.zero;
        if (ZInput.GetButton("Forward"))
        {
            move += Vector3.forward;
        }

        if (ZInput.GetButton("Backward"))
        {
            move += Vector3.back;
        }

        if (ZInput.GetButton("Left"))
        {
            move += Vector3.left;
        }

        if (ZInput.GetButton("Right"))
        {
            move += Vector3.right;
        }

        if (ZInput.GetButton("Jump"))
        {
            move += Vector3.up;
        }

        if (ZInput.GetButton("Crouch"))
        {
            move += Vector3.down;
        }

        running = ZInput.GetButton("Run");
        return move;
    }

    /// <summary>
    /// The new speed in the middle of the screen. A new message replaces the last one straight away, and it shows
    /// with the UI hidden too, as that's when the speed is set up for a shot.
    /// </summary>
    private static void ShowSpeed()
    {
        if (MessageHud.instance == null)
        {
            return;
        }

        string text = Localization.instance.Localize("$bettercamera_photo_speed", _speed.ToString("0.0", CultureInfo.CurrentCulture));
        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text, showDespiteHiddenHUD: true, log: false);
    }

    private static int RayMask
    {
        get
        {
            if (field == 0)
            {
                field = LayerMask.GetMask("Default", "static_solid", "terrain", "vehicle", "character", "piece",
                    "character_net", "viewblock");
            }

            return field;
        }
    }

    /// <summary>
    /// Keeps the camera looking at the point under the crosshair, or stops. When it stops, looking carries on from
    /// where the camera points, so it doesn't jump.
    /// </summary>
    private static void ToggleLookAt(Transform camera)
    {
        if (_lookAt != null)
        {
            _lookAt = null;
            Vector3 forward = camera.forward;
            _yaw = Quaternion.LookRotation(forward).eulerAngles.y;
            _pitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg, -MaxPitch, MaxPitch);
            return;
        }

        if (Physics.Raycast(camera.position, camera.forward, out RaycastHit hit, 10000f, RayMask))
        {
            _lookAt = hit.collider.transform;
            _lookAtPoint = _lookAt.InverseTransformPoint(hit.point);
        }
    }

    /// <summary>
    /// Moves the camera along with what's under the crosshair (such as a ship or a creature), or stops. Only its
    /// movement is followed, not its turning.
    /// </summary>
    private static void ToggleFollow(Transform camera)
    {
        if (_follow != null)
        {
            _follow = null;
            return;
        }

        if (Physics.Raycast(camera.position, camera.forward, out RaycastHit hit, 10000f, RayMask))
        {
            // The whole object (a ship, a creature) rather than the part that was hit.
            _follow = hit.collider.attachedRigidbody != null ? hit.collider.attachedRigidbody.transform : hit.collider.transform;
            _followOffset = camera.position - _follow.position;
        }
    }

    private static Vector3 KeepNearPlayer(Vector3 position)
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return position;
        }

        Vector3 centre = player.m_eye != null ? player.m_eye.position : player.transform.position;
        Vector3 offset = position - centre;
        float max = _maxDistance.Value;
        return offset.sqrMagnitude > max * max ? centre + (offset.normalized * max) : position;
    }
}
