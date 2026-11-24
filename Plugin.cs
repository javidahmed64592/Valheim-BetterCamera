using BepInEx;

using BetterCamera.Camera;
using BetterCamera.Combat;
using BetterCamera.Core;
using BetterCamera.FirstPerson;
using BetterCamera.Photo;
using BetterCamera.UI;

using HarmonyLib;

using Jotunn.Utils;

namespace BetterCamera;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid)]
[NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
public class BetterCameraPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "javidahmed64592.bettercamera";
    public const string PluginName = "Better Camera";
    public const string PluginVersion = "0.5.4";

    private Harmony _harmony;

    private void Awake()
    {
        CameraController.BindConfig(Config);
        ShipHud.BindConfig(Config);
        FirstPersonSettings.BindConfig(Config);
        PhotoMode.BindConfig(Config);
        MeleeAim.BindConfig(Config);
        Translations.Load();

        _harmony = new Harmony(PluginGUID);
        _harmony.PatchAll();
    }

    private void OnDestroy()
    {
        PhotoMode.Exit(Player.m_localPlayer, null);
        _harmony?.UnpatchSelf();
        FirstPersonBody.Restore();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Unity message")]
    private void Update()
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            PhotoMode.Exit(null, null);
            CameraController.Reset();
            return;
        }

        CameraController.UpdateInput(player);
        PhotoMode.UpdateInput(player);
    }
}
