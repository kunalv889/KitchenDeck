namespace KitchenDeck.API.Models;

/// <summary>
/// A platform user. Users are global; membership in a restaurant is modelled separately.
/// Stored as an individual JSON blob keyed by <see cref="Id"/>.
/// </summary>
public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash of the password (Base64).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Per-user random salt (Base64).</summary>
    public string PasswordSalt { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
