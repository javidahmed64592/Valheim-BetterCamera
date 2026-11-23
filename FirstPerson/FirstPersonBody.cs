using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace BetterCamera.FirstPerson;

/// <summary>
/// The local player's body in first person. The head is hidden from the camera but still casts its shadow, so the
/// shadow on the ground looks as it does in third person:
/// <list type="bullet">
/// <item>Everything attached to the head (helmet, hair, beard, breath) and the whole helmet item (hoods are
/// skinned to the body instead) only cast shadows.</item>
/// <item>The body, and any other gear skinned to the head, only cast shadows too. A copy of each with the head's
/// triangles cut out is what you see; it casts none.</item>
/// </list>
/// The arms and gear keep the game's own animations, as the copies share the body's bones. Lights worn on the head
/// (such as the Dvergr circlet's) point where you look. Gear changes rebuild it all, and everything is put back as
/// it was when leaving first person.
/// </summary>
internal static class FirstPersonBody
{
    /// <summary>
    /// A triangle is cut from the visible copy when any of its corners is at least this much moved by the head.
    /// </summary>
    private const float HeadWeightCutoff = 0.5f;

    /// <summary>
    /// A visible copy of a skinned renderer, with the source mesh it was cut from.
    /// </summary>
    private sealed class Copy
    {
        public SkinnedMeshRenderer Source;
        public Mesh SourceMesh;
        public SkinnedMeshRenderer Renderer;
    }

    private static readonly Dictionary<Renderer, ShadowCastingMode> ShadowOnly = [];
    private static readonly List<Copy> Copies = [];

    /// <summary>
    /// Meshes with the head cut out, by their source mesh. Null when there's nothing to cut or the mesh can't be
    /// read. Body meshes are shared by every player, so this is kept for the whole session.
    /// </summary>
    private static readonly Dictionary<Mesh, Mesh> Trimmed = [];

    /// <summary>
    /// Meshes that are all head (such as hair skinned to the body), so they need no visible copy.
    /// </summary>
    private static readonly HashSet<Mesh> AllHead = [];

    private static readonly List<Renderer> Renderers = [];
    private static readonly List<Material> SourceMaterials = [];
    private static readonly List<Material> CopyMaterials = [];
    private static MaterialPropertyBlock _block;

    private static Player _player;
    private static bool _applied;
    private static bool _dirty;

    /// <summary>
    /// Lights on the helmet slot that are being aimed, with their own rotation to put back.
    /// </summary>
    private static readonly Dictionary<Light, Quaternion> AimedLights = [];
    private static readonly List<Light> Lights = [];

    public static void Update(Player player, bool firstPerson)
    {
        NearFoliage.Update(firstPerson);
        if (!firstPerson || player == null || player.m_visual == null)
        {
            RestoreHead();
            RestoreLights();
            return;
        }

        AimHeadLights(player);

        if (!_applied || _dirty || _player != player || !CopiesCurrent())
        {
            RestoreHead();
            HideHead(player);
        }

        SyncCopies(player);
    }

    /// <summary>
    /// Called when a character's gear changes, so the local player's is set up again.
    /// </summary>
    public static void GearChanged(VisEquipment equipment)
    {
        Player player = Player.m_localPlayer;
        if (_applied && player != null && equipment == player.m_visEquipment)
        {
            _dirty = true;
        }
    }

    public static void Restore()
    {
        RestoreHead();
        RestoreLights();
        NearFoliage.Restore();
    }

    // ----- Head -----

    private static void HideHead(Player player)
    {
        _player = player;
        _applied = true;
        _dirty = false;

        Transform head = player.m_head;
        if (head == null)
        {
            return;
        }

        VisEquipment equipment = player.m_visEquipment;
        player.m_visual.GetComponentsInChildren(includeInactive: true, Renderers);
        foreach (Renderer renderer in Renderers)
        {
            if (renderer.transform.IsChildOf(head) || WornOnHead(equipment, renderer.transform))
            {
                MakeShadowOnly(renderer);
            }
            else if (renderer is SkinnedMeshRenderer skinned && SkinnedToHead(skinned, head))
            {
                Mesh trimmed = TrimmedMesh(skinned, head, out bool allHead);
                if (allHead)
                {
                    MakeShadowOnly(skinned);
                }
                else if (trimmed != null)
                {
                    MakeShadowOnly(skinned);
                    Copies.Add(MakeCopy(player, skinned, trimmed));
                }
            }
        }

        Renderers.Clear();
    }

    /// <summary>
    /// Whether the renderer is part of the helmet, hair or beard. Some of these are skinned to the body rather
    /// than attached to the head, so they aren't under it.
    /// </summary>
    private static bool WornOnHead(VisEquipment equipment, Transform renderer)
    {
        return equipment != null
            && (IsPartOf(renderer, equipment.m_helmetItemInstance)
                || IsPartOf(renderer, equipment.m_hairItemInstance)
                || IsPartOf(renderer, equipment.m_beardItemInstance));
    }

    private static bool IsPartOf(Transform renderer, GameObject item)
    {
        return item != null && renderer.IsChildOf(item.transform);
    }

    private static void MakeShadowOnly(Renderer renderer)
    {
        if (!ShadowOnly.ContainsKey(renderer))
        {
            ShadowOnly[renderer] = renderer.shadowCastingMode;
        }

        renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }

    private static bool SkinnedToHead(SkinnedMeshRenderer renderer, Transform head)
    {
        foreach (Transform bone in renderer.bones)
        {
            if (bone != null && bone.IsChildOf(head))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The renderer's mesh without the triangles the head moves, made once per mesh.
    /// </summary>
    private static Mesh TrimmedMesh(SkinnedMeshRenderer renderer, Transform head, out bool allHead)
    {
        Mesh mesh = renderer.sharedMesh;
        allHead = false;
        if (mesh == null)
        {
            return null;
        }

        if (!Trimmed.TryGetValue(mesh, out Mesh trimmed))
        {
            trimmed = mesh.isReadable ? CutHead(mesh, renderer.bones, head) : null;
            Trimmed[mesh] = trimmed;
        }

        allHead = AllHead.Contains(mesh);
        return trimmed;
    }

    private static Mesh CutHead(Mesh mesh, Transform[] bones, Transform head)
    {
        bool[] headBone = new bool[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            headBone[i] = bones[i] != null && bones[i].IsChildOf(head);
        }

        BoneWeight[] weights = mesh.boneWeights;
        bool[] onHead = new bool[weights.Length];
        for (int i = 0; i < weights.Length; i++)
        {
            BoneWeight w = weights[i];
            float onHeadWeight = HeadWeight(headBone, w.boneIndex0, w.weight0) + HeadWeight(headBone, w.boneIndex1, w.weight1)
                + HeadWeight(headBone, w.boneIndex2, w.weight2) + HeadWeight(headBone, w.boneIndex3, w.weight3);
            onHead[i] = onHeadWeight >= HeadWeightCutoff;
        }

        Mesh trimmed = Object.Instantiate(mesh);
        trimmed.name = mesh.name + " (first person)";
        int cut = 0;
        int left = 0;
        List<int> kept = [];
        for (int sub = 0; sub < mesh.subMeshCount; sub++)
        {
            if (mesh.GetTopology(sub) != MeshTopology.Triangles)
            {
                continue;
            }

            int[] triangles = mesh.GetTriangles(sub);
            kept.Clear();
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                if (onHead[triangles[t]] || onHead[triangles[t + 1]] || onHead[triangles[t + 2]])
                {
                    cut++;
                    continue;
                }

                kept.Add(triangles[t]);
                kept.Add(triangles[t + 1]);
                kept.Add(triangles[t + 2]);
            }

            left += kept.Count;
            trimmed.SetTriangles(kept, sub, calculateBounds: false);
        }

        if (cut > 0 && left == 0)
        {
            _ = AllHead.Add(mesh);
        }

        if (cut == 0 || left == 0)
        {
            Object.Destroy(trimmed);
            return null;
        }

        Jotunn.Logger.LogInfo($"First person: cut {cut} head triangles from {mesh.name}");
        return trimmed;
    }

    private static float HeadWeight(bool[] headBone, int index, float weight)
    {
        return index >= 0 && index < headBone.Length && headBone[index] ? weight : 0f;
    }

    // ----- Visible copies -----

    /// <summary>
    /// A copy of the renderer with the trimmed mesh, on the same bones and materials. It sits on the player's root
    /// rather than in the visual, so the game's LOD group (rebuilt from the visual's renderers) never takes it in.
    /// </summary>
    private static Copy MakeCopy(Player player, SkinnedMeshRenderer source, Mesh trimmed)
    {
        GameObject go = new(source.name + " (first person)") { layer = source.gameObject.layer };
        go.transform.SetParent(player.transform, worldPositionStays: false);

        SkinnedMeshRenderer renderer = go.AddComponent<SkinnedMeshRenderer>();
        renderer.sharedMesh = trimmed;
        renderer.bones = source.bones;
        renderer.rootBone = source.rootBone;
        renderer.localBounds = source.localBounds;
        renderer.updateWhenOffscreen = source.updateWhenOffscreen;
        renderer.quality = source.quality;
        renderer.sharedMaterials = source.sharedMaterials;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = source.receiveShadows;
        renderer.lightProbeUsage = source.lightProbeUsage;
        renderer.reflectionProbeUsage = source.reflectionProbeUsage;
        renderer.motionVectorGenerationMode = source.motionVectorGenerationMode;
        renderer.skinnedMotionVectors = source.skinnedMotionVectors;

        return new Copy { Source = source, SourceMesh = source.sharedMesh, Renderer = renderer };
    }

    /// <summary>
    /// Whether every copy still matches its source: the game swaps the body mesh when the model changes.
    /// </summary>
    private static bool CopiesCurrent()
    {
        foreach (Copy copy in Copies)
        {
            if (copy.Source == null || copy.Renderer == null || copy.Source.sharedMesh != copy.SourceMesh)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Keeps each copy in step with its source: place, shown or not, materials (armour textures) and the
    /// game's material properties (such as wetness and snow).
    /// </summary>
    private static void SyncCopies(Player player)
    {
        _block ??= new MaterialPropertyBlock();
        Vector3 rootScale = player.transform.lossyScale;
        foreach (Copy copy in Copies)
        {
            Transform source = copy.Source.transform;
            Transform target = copy.Renderer.transform;
            target.SetPositionAndRotation(source.position, source.rotation);
            Vector3 scale = source.lossyScale;
            target.localScale = new Vector3(scale.x / rootScale.x, scale.y / rootScale.y, scale.z / rootScale.z);

            copy.Renderer.enabled = copy.Source.enabled && copy.Source.gameObject.activeInHierarchy;

            copy.Source.GetSharedMaterials(SourceMaterials);
            copy.Renderer.GetSharedMaterials(CopyMaterials);
            if (!SameMaterials())
            {
                copy.Renderer.sharedMaterials = copy.Source.sharedMaterials;
            }

            copy.Source.GetPropertyBlock(_block);
            copy.Renderer.SetPropertyBlock(_block);
        }
    }

    private static bool SameMaterials()
    {
        if (SourceMaterials.Count != CopyMaterials.Count)
        {
            return false;
        }

        for (int i = 0; i < SourceMaterials.Count; i++)
        {
            if (SourceMaterials[i] != CopyMaterials[i])
            {
                return false;
            }
        }

        return true;
    }

    private static void RestoreHead()
    {
        foreach (KeyValuePair<Renderer, ShadowCastingMode> entry in ShadowOnly)
        {
            if (entry.Key != null)
            {
                entry.Key.shadowCastingMode = entry.Value;
            }
        }

        ShadowOnly.Clear();

        foreach (Copy copy in Copies)
        {
            if (copy.Renderer != null)
            {
                Object.Destroy(copy.Renderer.gameObject);
            }
        }

        Copies.Clear();
        _applied = false;
        _dirty = false;
        _player = null;
    }

    // ----- Head lights -----

    /// <summary>
    /// Points every light on the helmet slot (such as the Dvergr circlet's) along the camera's view, so it shines
    /// where the crosshair is. The head bone itself isn't turned: its resting angle depends on the animation, so a
    /// light carried by it would point somewhere else depending on when first person started.
    /// </summary>
    private static void AimHeadLights(Player player)
    {
        GameObject helmet = player.m_visEquipment != null ? player.m_visEquipment.m_helmetItemInstance : null;
        UnityEngine.Camera camera = Utils.GetMainCamera();
        if (helmet == null || camera == null)
        {
            RestoreLights();
            return;
        }

        helmet.GetComponentsInChildren(includeInactive: false, Lights);
        foreach (Light light in Lights)
        {
            if (!AimedLights.ContainsKey(light))
            {
                AimedLights[light] = light.transform.localRotation;
            }

            light.transform.rotation = camera.transform.rotation;
        }
    }

    private static void RestoreLights()
    {
        foreach (KeyValuePair<Light, Quaternion> entry in AimedLights)
        {
            if (entry.Key != null)
            {
                entry.Key.transform.localRotation = entry.Value;
            }
        }

        AimedLights.Clear();
    }
}
