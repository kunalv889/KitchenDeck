namespace KitchenDeck.API.Models;

/// <summary>
/// A single line on an order.
/// </summary>
public class OrderItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string MenuItemId { get; set; } = string.Empty;

    /// <summary>Snapshot of the menu item name at time of ordering.</summary>
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
    public OrderItemStatus Status { get; set; } = OrderItemStatus.Pending;
}

/// <summary>
/// An order (ticket) for a table, taken by a waiter.
/// Stored as an individual JSON blob keyed by <see cref="Id"/>.
/// </summary>
public class Order
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RestaurantId { get; set; } = string.Empty;
    public string TableId { get; set; } = string.Empty;
    public int TableNumber { get; set; }
    public string WaiterUserId { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Open;
    public List<OrderItem> Items { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
