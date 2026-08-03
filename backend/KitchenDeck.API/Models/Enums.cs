namespace KitchenDeck.API.Models;

/// <summary>
/// Roles/tags a member can hold within a single restaurant.
/// A member may hold more than one role.
/// </summary>
public enum StaffRole
{
    Admin,
    Cook,
    Waiter,
    Guard,
    CleaningStaff
}

/// <summary>
/// Lifecycle status of a whole order (a table's ticket).
/// </summary>
public enum OrderStatus
{
    Open,
    Preparing,
    Served,
    Closed,
    Cancelled
}

/// <summary>
/// Status of an individual line item on an order.
/// </summary>
public enum OrderItemStatus
{
    Pending,
    Preparing,
    Served
}
