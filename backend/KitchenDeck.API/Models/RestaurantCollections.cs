namespace KitchenDeck.API.Models;

/// <summary>
/// A restaurant's full menu, stored as one JSON blob keyed by <see cref="RestaurantId"/>.
/// </summary>
public class RestaurantMenu
{
    public string RestaurantId { get; set; } = string.Empty;
    public List<MenuItem> Items { get; set; } = new();
}

/// <summary>
/// A restaurant's full set of tables, stored as one JSON blob keyed by <see cref="RestaurantId"/>.
/// </summary>
public class RestaurantTables
{
    public string RestaurantId { get; set; } = string.Empty;
    public List<DiningTable> Tables { get; set; } = new();
}
