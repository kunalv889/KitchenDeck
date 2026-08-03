namespace KitchenDeck.API.Storage;

/// <summary>
/// Abstraction over a JSON-document store. The initial implementation is backed by
/// Azure Blob Storage (one blob per entity), but the interface keeps controllers and
/// services decoupled from the storage technology.
/// </summary>
public interface IJsonBlobStore
{
    /// <summary>Insert or replace an entity stored at <c>{container}/{id}.json</c>.</summary>
    Task UpsertAsync<T>(string container, string id, T entity, CancellationToken ct = default);

    /// <summary>Load a single entity, or <c>null</c> if it does not exist.</summary>
    Task<T?> GetAsync<T>(string container, string id, CancellationToken ct = default);

    /// <summary>Load every entity in a container.</summary>
    Task<IReadOnlyList<T>> ListAsync<T>(string container, CancellationToken ct = default);

    /// <summary>Load every entity whose blob name starts with <paramref name="prefix"/>.</summary>
    Task<IReadOnlyList<T>> ListByPrefixAsync<T>(string container, string prefix, CancellationToken ct = default);

    /// <summary>Delete an entity. No-op if it does not exist.</summary>
    Task<bool> DeleteAsync(string container, string id, CancellationToken ct = default);

    /// <summary>Returns whether an entity exists.</summary>
    Task<bool> ExistsAsync(string container, string id, CancellationToken ct = default);
}

/// <summary>
/// Well-known container (logical "table") names.
/// </summary>
public static class Containers
{
    public const string Users = "users";
    public const string Restaurants = "restaurants";
    public const string Members = "members";
    public const string Menu = "menu";
    public const string Tables = "tables";
    public const string Orders = "orders";
}
