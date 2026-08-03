using KitchenDeck.API.Models;
using KitchenDeck.API.Storage;

namespace KitchenDeck.API.Services;

/// <summary>
/// Restaurant and membership persistence/logic over the JSON blob store.
/// </summary>
public class RestaurantService
{
    private readonly IJsonBlobStore _store;

    public RestaurantService(IJsonBlobStore store)
    {
        _store = store;
    }

    public Task<Restaurant?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _store.GetAsync<Restaurant>(Containers.Restaurants, id, ct);

    public Task SaveAsync(Restaurant restaurant, CancellationToken ct = default) =>
        _store.UpsertAsync(Containers.Restaurants, restaurant.Id, restaurant, ct);

    public async Task<Restaurant> CreateAsync(string ownerUserId, string name, string? passcode, CancellationToken ct = default)
    {
        var restaurant = new Restaurant
        {
            Name = name.Trim(),
            OwnerUserId = ownerUserId
        };

        if (!string.IsNullOrWhiteSpace(passcode))
        {
            var (hash, salt) = PasswordHasher.Hash(passcode);
            restaurant.KitchenPasscodeHash = hash;
            restaurant.KitchenPasscodeSalt = salt;
        }

        await _store.UpsertAsync(Containers.Restaurants, restaurant.Id, restaurant, ct);

        // Owner is automatically an Admin member.
        var membership = new RestaurantMembership { RestaurantId = restaurant.Id };
        membership.Members.Add(new RestaurantMember
        {
            UserId = ownerUserId,
            Roles = new List<StaffRole> { StaffRole.Admin }
        });
        await _store.UpsertAsync(Containers.Members, restaurant.Id, membership, ct);

        return restaurant;
    }

    public async Task<RestaurantMembership> GetMembershipAsync(string restaurantId, CancellationToken ct = default)
    {
        var membership = await _store.GetAsync<RestaurantMembership>(Containers.Members, restaurantId, ct);
        return membership ?? new RestaurantMembership { RestaurantId = restaurantId };
    }

    public async Task SaveMembershipAsync(RestaurantMembership membership, CancellationToken ct = default) =>
        await _store.UpsertAsync(Containers.Members, membership.RestaurantId, membership, ct);

    /// <summary>Restaurants where the given user is a member (owner included).</summary>
    public async Task<IReadOnlyList<Restaurant>> ListForUserAsync(string userId, CancellationToken ct = default)
    {
        var memberships = await _store.ListAsync<RestaurantMembership>(Containers.Members, ct);
        var restaurantIds = memberships
            .Where(m => m.Members.Any(mem => mem.UserId == userId))
            .Select(m => m.RestaurantId)
            .ToList();

        var restaurants = new List<Restaurant>();
        foreach (var id in restaurantIds)
        {
            var restaurant = await GetByIdAsync(id, ct);
            if (restaurant is not null)
            {
                restaurants.Add(restaurant);
            }
        }

        return restaurants.OrderBy(r => r.Name).ToList();
    }

    public async Task<IReadOnlyList<StaffRole>> GetRolesAsync(string restaurantId, string userId, CancellationToken ct = default)
    {
        var membership = await GetMembershipAsync(restaurantId, ct);
        var member = membership.Members.FirstOrDefault(m => m.UserId == userId);
        return member?.Roles ?? new List<StaffRole>();
    }

    public async Task<bool> IsAdminAsync(string restaurantId, string userId, CancellationToken ct = default)
    {
        var restaurant = await GetByIdAsync(restaurantId, ct);
        if (restaurant is null)
        {
            return false;
        }

        if (restaurant.OwnerUserId == userId)
        {
            return true;
        }

        var roles = await GetRolesAsync(restaurantId, userId, ct);
        return roles.Contains(StaffRole.Admin);
    }

    /// <summary>Whether the user is the owner or any kind of member of the restaurant.</summary>
    public async Task<bool> IsMemberAsync(string restaurantId, string userId, CancellationToken ct = default)
    {
        var restaurant = await GetByIdAsync(restaurantId, ct);
        if (restaurant is null)
        {
            return false;
        }

        if (restaurant.OwnerUserId == userId)
        {
            return true;
        }

        var membership = await GetMembershipAsync(restaurantId, ct);
        return membership.Members.Any(m => m.UserId == userId);
    }

    /// <summary>Whether the user is the owner/Admin, or holds at least one of the given roles.</summary>
    public async Task<bool> HasAnyRoleAsync(string restaurantId, string userId, IEnumerable<StaffRole> roles, CancellationToken ct = default)
    {
        if (await IsAdminAsync(restaurantId, userId, ct))
        {
            return true;
        }

        var userRoles = await GetRolesAsync(restaurantId, userId, ct);
        return roles.Any(userRoles.Contains);
    }

    /// <summary>Adds or updates a member's roles. Returns false if the user is already present with the same intent.</summary>
    public async Task AddOrUpdateMemberAsync(string restaurantId, string userId, List<StaffRole> roles, CancellationToken ct = default)
    {
        var membership = await GetMembershipAsync(restaurantId, ct);
        var member = membership.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
        {
            membership.Members.Add(new RestaurantMember { UserId = userId, Roles = roles });
        }
        else
        {
            member.Roles = roles;
        }

        await SaveMembershipAsync(membership, ct);
    }

    public async Task RemoveMemberAsync(string restaurantId, string userId, CancellationToken ct = default)
    {
        var membership = await GetMembershipAsync(restaurantId, ct);
        membership.Members.RemoveAll(m => m.UserId == userId);
        await SaveMembershipAsync(membership, ct);
    }

    public bool VerifyKitchenPasscode(Restaurant restaurant, string passcode)
    {
        if (restaurant.KitchenPasscodeHash is null || restaurant.KitchenPasscodeSalt is null)
        {
            return false;
        }

        return PasswordHasher.Verify(passcode, restaurant.KitchenPasscodeHash, restaurant.KitchenPasscodeSalt);
    }
}
