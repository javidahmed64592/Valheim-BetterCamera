using UnityEngine;

namespace BetterCamera.FirstPerson;

/// <summary>
/// In first person, attacks start from the hand, to one side of the eyes, while vanilla aims them parallel to
/// the view, so they land beside the crosshair. This aims them from where they start at whatever the crosshair
/// is on instead (or at a point far along the view when it's on nothing).
/// </summary>
internal static class CrosshairAim
{
    private const float MaxDistance = 300f;

    private static readonly RaycastHit[] Hits = new RaycastHit[32];

    private static int Mask
    {
        get
        {
            if (field == 0)
            {
                field = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle",
                    "character", "character_net", "character_ghost", "character_noenv", "hitbox");
            }

            return field;
        }
    }

    /// <summary>
    /// The direction from `from` to what the crosshair is on.
    /// </summary>
    public static Vector3 DirectionFrom(Player player, Vector3 from, Vector3 fallback)
    {
        if (!TryGetTarget(player, MaxDistance, out Vector3 target))
        {
            return fallback;
        }

        Vector3 direction = target - from;
        return direction.sqrMagnitude > 0.01f ? direction.normalized : fallback;
    }

    /// <summary>
    /// The point the crosshair is on, or the point `maxDistance` along the view when nothing is nearer. Either way
    /// it's on the line through the crosshair.
    /// </summary>
    public static bool TryGetTarget(Player player, float maxDistance, out Vector3 target)
    {
        UnityEngine.Camera camera = Utils.GetMainCamera();
        if (camera == null)
        {
            target = Vector3.zero;
            return false;
        }

        Vector3 origin = camera.transform.position;
        Vector3 forward = camera.transform.forward;
        target = origin + (forward * maxDistance);

        int count = Physics.RaycastNonAlloc(origin, forward, Hits, maxDistance, Mask, QueryTriggerInteraction.Ignore);
        float nearest = maxDistance;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = Hits[i];
            if (hit.distance < nearest && !IsOwn(player, hit.collider))
            {
                nearest = hit.distance;
                target = hit.point;
            }
        }

        return true;
    }

    /// <summary>
    /// The player's own colliders (body, held items, the ship or mount they're on) mustn't catch the aim.
    /// </summary>
    private static bool IsOwn(Player player, Collider collider)
    {
        Transform transform = collider.transform;
        return transform.IsChildOf(player.transform)
            || (player.GetStandingOnShip() is Ship ship && transform.IsChildOf(ship.transform))
            || (player.m_doodadController is Sadle sadle && sadle.GetCharacter() != null
                && transform.IsChildOf(sadle.GetCharacter().transform));
    }
}
