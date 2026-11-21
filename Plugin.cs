using BepInEx;

using BetterCamera.Camera;
using BetterCamera.Core;

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
    public const string PluginVersion = "0.1.0";

    private Harmony _harmony;

    private void Awake()
    {
        CameraController.BindConfig(Config);
        Translations.Load();

        _harmony = new Harmony(PluginGUID);
        _harmony.PatchAll();
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        FirstPersonBody.Restore();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Unity message")]
    private void Update()
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            CameraController.Reset();
            return;
        }

        CameraController.UpdateInput(player);
    }
}
