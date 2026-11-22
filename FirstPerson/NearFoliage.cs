using System.Collections.Generic;

using UnityEngine;

namespace BetterCamera.FirstPerson;

/// <summary>
/// Keeps bushes and other foliage visible when the camera is right next to them in first person. Valheim's
/// vegetation shader hides a mesh when the camera gets close (the material's <c>_CamCull</c> setting, on berry
/// bushes for example), which keeps the third-person camera from being blocked by leaves but makes plants vanish in
/// front of your eyes in first person. While in first person that setting is turned off on every loaded material,
/// rescanning now and then for materials loaded since, and the original values are put back when leaving it.
/// </summary>
internal static class NearFoliage
{
    private const float RescanInterval = 3f;

    private static readonly int CamCull = Shader.PropertyToID("_CamCull");

    /// <summary>
    /// Materials whose camera cull was turned off, with the value to put back.
    /// </summary>
    private static readonly Dictionary<Material, float> Changed = [];

    /// <summary>
    /// Instance ids of materials already looked at, so a rescan only checks new ones.
    /// </summary>
    private static readonly HashSet<int> Checked = [];

    private static float _nextScan;

    public static void Update(bool firstPerson)
    {
        if (!firstPerson)
        {
            Restore();
            return;
        }

        if (Time.unscaledTime < _nextScan)
        {
            return;
        }

        _nextScan = Time.unscaledTime + RescanInterval;
        foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (!Checked.Add(material.GetInstanceID()) || !material.HasProperty(CamCull))
            {
                continue;
            }

            float value = material.GetFloat(CamCull);
            if (value != 0f)
            {
                Changed[material] = value;
                material.SetFloat(CamCull, 0f);
            }
        }
    }

    public static void Restore()
    {
        if (Checked.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<Material, float> entry in Changed)
        {
            if (entry.Key != null)
            {
                entry.Key.SetFloat(CamCull, entry.Value);
            }
        }

        Changed.Clear();
        Checked.Clear();
        _nextScan = 0f;
    }
}
