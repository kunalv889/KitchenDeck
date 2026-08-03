namespace KitchenDeck.API.Models;

/// <summary>
/// The full staff roster for a single restaurant, stored as one JSON blob keyed by
/// <see cref="RestaurantId"/> so the roster loads in a single read.
/// </summary>
public class RestaurantMembership
{
    public string RestaurantId { get; set; } = string.Empty;
    public List<RestaurantMember> Members { get; set; } = new();
}
