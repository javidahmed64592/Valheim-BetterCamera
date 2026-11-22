using UnityEngine;

namespace BetterCamera.Combat;

/// <summary>
/// Bends the local player's upper body up or down during melee swings, so the animation matches the tilted swing
/// from <see cref="MeleeAim"/> instead of slashing level while hitting something above or below. The bend is
/// spread over the spine bones after the animator has posed them, and eases in and out around the swing.
/// The player's animator runs with physics, so on frames without a physics step the bones still hold last
/// frame's bent pose; the bend is undone first then, so it never builds up.
/// </summary>
internal static class MeleeLean
{
    private const float EaseTime = 0.15f;

    private static readonly HumanBodyBones[] SpineBones =
        [HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest];

    private static readonly Transform[] Bones = new Transform[SpineBones.Length];
    private static readonly Quaternion[] Unbent = new Quaternion[SpineBones.Length];
    private static readonly Quaternion[] Bent = new Quaternion[SpineBones.Length];
    private static bool _bent;
    private static Animator _bonesFor;
    private static float _weight;
    private static float _angle;
    private static Vector3 _unbentHead;
    private static int _unbentHeadFrame = -1;

    /// <summary>
    /// The head bone's position before this frame's bend. The first-person camera rides on the head, so it reads
    /// this to stay put while the body leans.
    /// </summary>
    public static Vector3 HeadPosition(Player player)
    {
        return _unbentHeadFrame == Time.frameCount ? _unbentHead : player.m_head.position;
    }

    /// <summary>Runs every frame after the animator, for the local player.</summary>
    public static void Apply(Player player, float dt)
    {
        bool swinging = MeleeAim.MaxAngle > 0f && MeleeAim.LeanIntoSwings && IsMeleeSwing(player);
        if (swinging)
        {
            _angle = Pitch(player);
        }

        _weight = Mathf.MoveTowards(_weight, swinging ? 1f : 0f, dt / EaseTime);
        if (player.m_head == null || !FindBones(player))
        {
            return;
        }

        Unbend();
        if (_weight <= 0f)
        {
            return;
        }

        _unbentHead = player.m_head.position;
        _unbentHeadFrame = Time.frameCount;

        // Rotating about the body's right axis by a negative angle tips forward upwards. Each bone takes a share,
        // in world space, so the bend adds up along the spine like a real lean.
        float share = -_angle * Mathf.SmoothStep(0f, 1f, _weight) / Bones.Length;
        Quaternion bend = Quaternion.AngleAxis(share, player.transform.right);
        for (int i = 0; i < Bones.Length; i++)
        {
            Transform bone = Bones[i];
            if (bone == null)
            {
                continue;
            }

            Unbent[i] = bone.localRotation;
            bone.rotation = bend * bone.rotation;
            Bent[i] = bone.localRotation;
        }

        _bent = true;
    }

    /// <summary>
    /// Puts back the pose from before last frame's bend, on bones the animator hasn't posed again since.
    /// </summary>
    private static void Unbend()
    {
        if (!_bent)
        {
            return;
        }

        _bent = false;
        for (int i = 0; i < Bones.Length; i++)
        {
            Transform bone = Bones[i];
            if (bone != null && bone.localRotation == Bent[i])
            {
                bone.localRotation = Unbent[i];
            }
        }
    }

    private static bool IsMeleeSwing(Player player)
    {
        Attack attack = player.m_currentAttack;
        return attack != null && player.InAttack() && !player.IsAttached()
            && (attack.m_attackType == Attack.AttackType.Horizontal || attack.m_attackType == Attack.AttackType.Vertical);
    }

    /// <summary>How far the swing tilts, in degrees above level, the same limit MeleeAim uses.</summary>
    private static float Pitch(Player player)
    {
        Vector3 look = player.GetLookDir();
        float pitch = Mathf.Asin(Mathf.Clamp(look.y, -1f, 1f)) * Mathf.Rad2Deg;
        float max = Mathf.Max(player.m_currentAttack.m_maxYAngle, MeleeAim.MaxAngle);
        return Mathf.Clamp(pitch, -max, max);
    }

    private static bool FindBones(Player player)
    {
        Animator animator = player.m_animator;
        if (animator == null)
        {
            return false;
        }

        if (_bonesFor != animator)
        {
            _bonesFor = animator;
            _bent = false;
            for (int i = 0; i < SpineBones.Length; i++)
            {
                Bones[i] = Utils.GetBoneTransform(animator, SpineBones[i]);
            }
        }

        return Bones[0] != null;
    }
}
