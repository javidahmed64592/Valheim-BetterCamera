using BepInEx.Configuration;

using UnityEngine;

namespace BetterCamera.Combat;

/// <summary>
/// Vanilla melee swings sweep level with your body (every player weapon has a max vertical angle of 0), so an
/// enemy above or below you on a slope is missed however you look at it. This lets the swing tilt up or down
/// with your view, in first and third person, up to a configurable angle.
/// </summary>
internal static class MeleeAim
{
    private static ConfigEntry<float> _maxAngle;
    private static ConfigEntry<bool> _leanIntoSwings;

    /// <summary>
    /// The furthest a melee swing tilts up or down to follow your view, in degrees. 0 is vanilla.
    /// </summary>
    public static float MaxAngle => _maxAngle.Value;

    /// <summary>
    /// Whether your upper body bends with tilted swings (see <see cref="MeleeLean"/>).
    /// </summary>
    public static bool LeanIntoSwings => _leanIntoSwings.Value;

    public static void BindConfig(ConfigFile config)
    {
        _maxAngle = config.Bind("Combat", "Melee Aim Angle", 45f,
            new ConfigDescription("How far, in degrees, melee swings tilt up or down to follow where you look, so " +
                "you can hit enemies above or below you on a slope. Vanilla swings always stay level (0).",
                new AcceptableValueRange<float>(0f, 90f)));
        _leanIntoSwings = config.Bind("Combat", "Lean Into Swings", true,
            "Your upper body bends up or down with tilted melee swings, so the animation matches where the swing " +
            "goes. Only you see it; other players see the normal animation.");
    }

    /// <summary>
    /// Vanilla's melee direction, built the same way but with the larger of the attack's own angle and ours.
    /// </summary>
    public static Vector3 Direction(Attack attack, Transform originJoint, Vector3 vanilla)
    {
        float maxAngle = Mathf.Max(attack.m_maxYAngle, MaxAngle);
        if (maxAngle <= attack.m_maxYAngle)
        {
            return vanilla;
        }

        // In first person GetAimDir already points at what the crosshair is on (see CrosshairAim).
        Vector3 forward = attack.m_character.transform.forward;
        Vector3 aim = attack.m_character.GetAimDir(originJoint.position);
        aim.x = forward.x;
        aim.z = forward.z;
        aim.Normalize();
        return Vector3.RotateTowards(forward, aim, Mathf.Deg2Rad * maxAngle, 10f);
    }
}
