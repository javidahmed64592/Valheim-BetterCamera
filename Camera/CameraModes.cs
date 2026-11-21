namespace BetterCamera.Camera;

/// <summary>
/// How the player is getting around, which picks the zoom limits and the views on offer.
/// </summary>
internal enum TravelMode { Walking = 0, Sailing = 1, Riding = 2 }

/// <summary>
/// What the camera orbits while steering a ship.
/// </summary>
internal enum ViewAnchor
{
    /// <summary>
    /// The player's head, as in vanilla.
    /// </summary>
    Player = 0,

    /// <summary>
    /// The middle of the ship, raised a few metres.
    /// </summary>
    Centre = 1,
}
