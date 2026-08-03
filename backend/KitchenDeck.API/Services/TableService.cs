using KitchenDeck.API.Models;
using KitchenDeck.API.Storage;

namespace KitchenDeck.API.Services;

/// <summary>
/// Dining-table persistence over the JSON blob store (one blob per restaurant).
/// </summary>
public class TableService
{
    private readonly IJsonBlobStore _store;

    public TableService(IJsonBlobStore store)
    {
        _store = store;
    }

    private async Task<RestaurantTables> LoadAsync(string restaurantId, CancellationToken ct)
    {
        var tables = await _store.GetAsync<RestaurantTables>(Containers.Tables, restaurantId, ct);
        return tables ?? new RestaurantTables { RestaurantId = restaurantId };
    }

    private Task SaveAsync(RestaurantTables tables, CancellationToken ct) =>
        _store.UpsertAsync(Containers.Tables, tables.RestaurantId, tables, ct);

    public async Task<IReadOnlyList<DiningTable>> ListAsync(string restaurantId, CancellationToken ct = default)
    {
        var tables = await LoadAsync(restaurantId, ct);
        return tables.Tables.OrderBy(t => t.Number).ToList();
    }

    /// <summary>Adds a table. Returns null if the table number already exists.</summary>
    public async Task<DiningTable?> AddAsync(string restaurantId, DiningTable table, CancellationToken ct = default)
    {
        var tables = await LoadAsync(restaurantId, ct);
        if (tables.Tables.Any(t => t.Number == table.Number))
        {
            return null;
        }

        table.Id = Guid.NewGuid().ToString("N");
        table.RestaurantId = restaurantId;
        tables.Tables.Add(table);
        await SaveAsync(tables, ct);
        return table;
    }

    /// <summary>Updates a table. Returns (null, false) if not found, (null, true) if number clashes.</summary>
    public async Task<(DiningTable? table, bool numberConflict)> UpdateAsync(
        string restaurantId, string tableId, DiningTable updated, CancellationToken ct = default)
    {
        var tables = await LoadAsync(restaurantId, ct);
        var existing = tables.Tables.FirstOrDefault(t => t.Id == tableId);
        if (existing is null)
        {
            return (null, false);
        }

        if (tables.Tables.Any(t => t.Id != tableId && t.Number == updated.Number))
        {
            return (null, true);
        }

        existing.Number = updated.Number;
        existing.Label = updated.Label;
        existing.Seats = updated.Seats;
        await SaveAsync(tables, ct);
        return (existing, false);
    }

    public async Task<bool> DeleteAsync(string restaurantId, string tableId, CancellationToken ct = default)
    {
        var tables = await LoadAsync(restaurantId, ct);
        var removed = tables.Tables.RemoveAll(t => t.Id == tableId) > 0;
        if (removed)
        {
            await SaveAsync(tables, ct);
        }
        return removed;
    }
}
