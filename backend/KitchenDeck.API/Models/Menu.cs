namespace KitchenDeck.API.Models;

/// <summary>
/// A menu item belonging to a restaurant.
/// </summary>
public class MenuItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RestaurantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
}

/// <summary>
/// A dining table belonging to a restaurant.
/// </summary>
public class DiningTable
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RestaurantId { get; set; } = string.Empty;

    /// <summary>Human-facing table number (unique within a restaurant).</summary>
    public int Number { get; set; }
    public string? Label { get; set; }
    public int Seats { get; set; }
}
