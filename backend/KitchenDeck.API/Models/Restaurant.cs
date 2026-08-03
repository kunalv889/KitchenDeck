namespace KitchenDeck.API.Models;

/// <summary>
/// A restaurant owned by the user who created it. The owner is implicitly an Admin.
/// Stored as an individual JSON blob keyed by <see cref="Id"/>.
/// </summary>
public class Restaurant
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash of the 6-digit kitchen-window passcode (Base64).</summary>
    public string? KitchenPasscodeHash { get; set; }

    /// <summary>Salt for the kitchen passcode (Base64).</summary>
    public string? KitchenPasscodeSalt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Association between a <see cref="User"/> and a <see cref="Restaurant"/> with one or more roles.
/// Stored per restaurant as a JSON blob so a restaurant's staff list loads in one read.
/// </summary>
public class RestaurantMember
{
    public string UserId { get; set; } = string.Empty;
    public List<StaffRole> Roles { get; set; } = new();
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
