using KitchenDeck.API.Models;
using KitchenDeck.API.Storage;

namespace KitchenDeck.API.Services;

/// <summary>
/// Order persistence and logic over the JSON blob store. Orders are stored one blob
/// per order under the key <c>{restaurantId}/{orderId}</c>, so a restaurant's orders
/// can be listed efficiently by blob-name prefix.
/// </summary>
public class OrderService
{
    private readonly IJsonBlobStore _store;
    private readonly MenuService _menu;
    private readonly TableService _tables;

    public OrderService(IJsonBlobStore store, MenuService menu, TableService tables)
    {
        _store = store;
        _menu = menu;
        _tables = tables;
    }

    private static string Key(string restaurantId, string orderId) => $"{restaurantId}/{orderId}";

    public Task<Order?> GetAsync(string restaurantId, string orderId, CancellationToken ct = default) =>
        _store.GetAsync<Order>(Containers.Orders, Key(restaurantId, orderId), ct);

    private Task SaveAsync(Order order, CancellationToken ct)
    {
        order.UpdatedAt = DateTimeOffset.UtcNow;
        return _store.UpsertAsync(Containers.Orders, Key(order.RestaurantId, order.Id), order, ct);
    }

    public async Task<IReadOnlyList<Order>> ListForRestaurantAsync(
        string restaurantId, bool activeOnly, CancellationToken ct = default)
    {
        var orders = await _store.ListByPrefixAsync<Order>(Containers.Orders, $"{restaurantId}/", ct);
        IEnumerable<Order> query = orders;
        if (activeOnly)
        {
            query = query.Where(o => o.Status is not (OrderStatus.Closed or OrderStatus.Cancelled));
        }
        return query.OrderBy(o => o.CreatedAt).ToList();
    }

    /// <summary>Builds order lines from menu-item references, snapshotting name and price.</summary>
    private async Task<List<OrderItem>> BuildLinesAsync(
        string restaurantId, IEnumerable<(string menuItemId, int quantity, string? notes)> lines, CancellationToken ct)
    {
        var menu = await _menu.ListAsync(restaurantId, ct);
        var result = new List<OrderItem>();
        foreach (var (menuItemId, quantity, notes) in lines)
        {
            var menuItem = menu.FirstOrDefault(m => m.Id == menuItemId);
            if (menuItem is null || quantity <= 0)
            {
                continue;
            }

            result.Add(new OrderItem
            {
                MenuItemId = menuItem.Id,
                Name = menuItem.Name,
                UnitPrice = menuItem.Price,
                Quantity = quantity,
                Notes = notes,
                Status = OrderItemStatus.Pending
            });
        }
        return result;
    }

    public async Task<Order?> CreateAsync(
        string restaurantId, string tableId, string waiterUserId,
        IEnumerable<(string menuItemId, int quantity, string? notes)> lines, CancellationToken ct = default)
    {
        var table = (await _tables.ListAsync(restaurantId, ct)).FirstOrDefault(t => t.Id == tableId);
        if (table is null)
        {
            return null;
        }

        var order = new Order
        {
            RestaurantId = restaurantId,
            TableId = table.Id,
            TableNumber = table.Number,
            WaiterUserId = waiterUserId,
            Status = OrderStatus.Open,
            Items = await BuildLinesAsync(restaurantId, lines, ct)
        };

        await SaveAsync(order, ct);
        return order;
    }

    public async Task<Order?> AddLinesAsync(
        string restaurantId, string orderId,
        IEnumerable<(string menuItemId, int quantity, string? notes)> lines, CancellationToken ct = default)
    {
        var order = await GetAsync(restaurantId, orderId, ct);
        if (order is null)
        {
            return null;
        }

        order.Items.AddRange(await BuildLinesAsync(restaurantId, lines, ct));
        await SaveAsync(order, ct);
        return order;
    }

    public async Task<(Order? order, bool lineFound)> UpdateLineAsync(
        string restaurantId, string orderId, string lineId, int quantity, string? notes, CancellationToken ct = default)
    {
        var order = await GetAsync(restaurantId, orderId, ct);
        if (order is null)
        {
            return (null, false);
        }

        var line = order.Items.FirstOrDefault(i => i.Id == lineId);
        if (line is null)
        {
            return (order, false);
        }

        if (quantity <= 0)
        {
            order.Items.Remove(line);
        }
        else
        {
            line.Quantity = quantity;
            line.Notes = notes;
        }

        await SaveAsync(order, ct);
        return (order, true);
    }

    public async Task<(Order? order, bool lineFound)> SetLineStatusAsync(
        string restaurantId, string orderId, string lineId, OrderItemStatus status, CancellationToken ct = default)
    {
        var order = await GetAsync(restaurantId, orderId, ct);
        if (order is null)
        {
            return (null, false);
        }

        var line = order.Items.FirstOrDefault(i => i.Id == lineId);
        if (line is null)
        {
            return (order, false);
        }

        line.Status = status;
        RecalculateOrderStatus(order);
        await SaveAsync(order, ct);
        return (order, true);
    }

    public async Task<(Order? order, bool lineFound)> RemoveLineAsync(
        string restaurantId, string orderId, string lineId, CancellationToken ct = default)
    {
        var order = await GetAsync(restaurantId, orderId, ct);
        if (order is null)
        {
            return (null, false);
        }

        var removed = order.Items.RemoveAll(i => i.Id == lineId) > 0;
        if (removed)
        {
            await SaveAsync(order, ct);
        }
        return (order, removed);
    }

    public async Task<Order?> SetStatusAsync(
        string restaurantId, string orderId, OrderStatus status, CancellationToken ct = default)
    {
        var order = await GetAsync(restaurantId, orderId, ct);
        if (order is null)
        {
            return null;
        }

        order.Status = status;
        // When the whole order is advanced, cascade to its lines for consistency.
        if (status == OrderStatus.Preparing)
        {
            foreach (var line in order.Items.Where(i => i.Status == OrderItemStatus.Pending))
            {
                line.Status = OrderItemStatus.Preparing;
            }
        }
        else if (status == OrderStatus.Served)
        {
            foreach (var line in order.Items)
            {
                line.Status = OrderItemStatus.Served;
            }
        }

        await SaveAsync(order, ct);
        return order;
    }

    /// <summary>Keeps the order status in sync with its line statuses.</summary>
    private static void RecalculateOrderStatus(Order order)
    {
        if (order.Status is OrderStatus.Closed or OrderStatus.Cancelled || order.Items.Count == 0)
        {
            return;
        }

        if (order.Items.All(i => i.Status == OrderItemStatus.Served))
        {
            order.Status = OrderStatus.Served;
        }
        else if (order.Items.Any(i => i.Status is OrderItemStatus.Preparing or OrderItemStatus.Served))
        {
            order.Status = OrderStatus.Preparing;
        }
        else
        {
            order.Status = OrderStatus.Open;
        }
    }
}
