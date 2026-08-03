using KitchenDeck.API.Models;
using KitchenDeck.API.Storage;

namespace KitchenDeck.API.Services;

/// <summary>
/// User persistence helpers over the JSON blob store.
/// </summary>
public class UserService
{
    private readonly IJsonBlobStore _store;

    public UserService(IJsonBlobStore store)
    {
        _store = store;
    }

    public Task<User?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _store.GetAsync<User>(Containers.Users, id, ct);

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var users = await _store.ListAsync<User>(Containers.Users, ct);
        return users.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    public Task SaveAsync(User user, CancellationToken ct = default) =>
        _store.UpsertAsync(Containers.Users, user.Id, user, ct);
}
