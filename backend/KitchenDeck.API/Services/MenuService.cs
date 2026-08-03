using KitchenDeck.API.Models;
using KitchenDeck.API.Storage;

namespace KitchenDeck.API.Services;

/// <summary>
/// Menu persistence over the JSON blob store (one blob per restaurant).
/// </summary>
public class MenuService
{
    private readonly IJsonBlobStore _store;

    public MenuService(IJsonBlobStore store)
    {
        _store = store;
    }

    private async Task<RestaurantMenu> LoadAsync(string restaurantId, CancellationToken ct)
    {
        var menu = await _store.GetAsync<RestaurantMenu>(Containers.Menu, restaurantId, ct);
        return menu ?? new RestaurantMenu { RestaurantId = restaurantId };
    }

    private Task SaveAsync(RestaurantMenu menu, CancellationToken ct) =>
        _store.UpsertAsync(Containers.Menu, menu.RestaurantId, menu, ct);

    public async Task<IReadOnlyList<MenuItem>> ListAsync(string restaurantId, CancellationToken ct = default)
    {
        var menu = await LoadAsync(restaurantId, ct);
        return menu.Items;
    }

    public async Task<MenuItem> AddAsync(string restaurantId, MenuItem item, CancellationToken ct = default)
    {
        var menu = await LoadAsync(restaurantId, ct);
        item.Id = Guid.NewGuid().ToString("N");
        item.RestaurantId = restaurantId;
        menu.Items.Add(item);
        await SaveAsync(menu, ct);
        return item;
    }

    public async Task<MenuItem?> UpdateAsync(string restaurantId, string itemId, MenuItem updated, CancellationToken ct = default)
    {
        var menu = await LoadAsync(restaurantId, ct);
        var existing = menu.Items.FirstOrDefault(i => i.Id == itemId);
        if (existing is null)
        {
            return null;
        }

        existing.Name = updated.Name;
        existing.Description = updated.Description;
        existing.Category = updated.Category;
        existing.Price = updated.Price;
        existing.IsAvailable = updated.IsAvailable;
        await SaveAsync(menu, ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(string restaurantId, string itemId, CancellationToken ct = default)
    {
        var menu = await LoadAsync(restaurantId, ct);
        var removed = menu.Items.RemoveAll(i => i.Id == itemId) > 0;
        if (removed)
        {
            await SaveAsync(menu, ct);
        }
        return removed;
    }
}
