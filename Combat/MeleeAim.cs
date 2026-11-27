using BepInEx.Configuration;

using BetterCamera.FirstPerson;

using UnityEngine;

namespace BetterCamera.Combat;

/// <summary>
/// Vanilla melee swings sweep level with your body (every player weapon has a max vertical angle of 0), so an
/// enemy above or below you on a slope is missed however you look at it. This lets the swing tilt up or down
/// with your view, in first and third person, up to a configurable angle.
/// </summary>
internal static class MeleeAim
{
    /// <summary>
    /// Tilting melee swings with the view and leaning the upper body into them. Without it, swings stay level as in
    /// vanilla.
    /// </summary>
    public const string Feature = "Melee aim with view";

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
    /// Vanilla's melee direction, built the same way but with the larger of the attack's own angle and ours. In
    /// first person the swing is aimed at the crosshair instead (see <see cref="FirstPersonDirection"/>).
    /// </summary>
    public static Vector3 Direction(Attack attack, Transform originJoint, Vector3 vanilla)
    {
        float maxAngle = Mathf.Max(attack.m_maxYAngle, MaxAngle);
        if (FirstPersonSettings.Active && attack.m_character is Player player)
        {
            return FirstPersonDirection(attack, player, originJoint, maxAngle);
        }

        if (maxAngle <= attack.m_maxYAngle)
        {
            return vanilla;
        }

        Vector3 forward = attack.m_character.transform.forward;
        Vector3 aim = attack.m_character.GetAimDir(originJoint.position);
        aim.x = forward.x;
        aim.z = forward.z;
        aim.Normalize();
        return Vector3.RotateTowards(forward, aim, Mathf.Deg2Rad * maxAngle, 10f);
    }

    /// <summary>
    /// The swing starts at the attack's height above the origin (chest height, well below the eyes), so a swing
    /// parallel to the view, or aimed from the origin, passes below or beside the crosshair. This tilts it so it
    /// goes from where it starts through the crosshair, at what the crosshair is on or a point on the crosshair line
    /// just past the swing's reach. It still sweeps where the body faces, and tilts at most `maxAngle`.
    /// </summary>
    private static Vector3 FirstPersonDirection(Attack attack, Player player, Transform originJoint, float maxAngle)
    {
        Vector3 forward = player.transform.forward;
        Vector3 start = originJoint.position + (Vector3.up * attack.m_attackHeight)
            + (player.transform.right * attack.m_attackOffset);
        if (!CrosshairAim.TryGetTarget(player, attack.m_attackRange + 1f, out Vector3 target))
        {
            return forward;
        }

        Vector3 toTarget = target - start;
        float level = Vector3.ProjectOnPlane(toTarget, Vector3.up).magnitude;
        float pitch = Mathf.Atan2(toTarget.y, Mathf.Max(level, 0.1f)) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, -maxAngle, maxAngle);
        return Quaternion.AngleAxis(-pitch, player.transform.right) * forward;
    }
}
