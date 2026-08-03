using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace KitchenDeck.API.Storage;

/// <summary>
/// Azure Blob Storage backed implementation of <see cref="IJsonBlobStore"/>.
/// Each logical container maps to a blob container; each entity is one JSON blob
/// named <c>{id}.json</c>.
/// </summary>
public class AzureBlobStore : IJsonBlobStore
{
    private readonly BlobServiceClient _client;
    private static readonly ConcurrentDictionary<string, bool> _ensuredContainers = new();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AzureBlobStore(BlobServiceClient client)
    {
        _client = client;
    }

    private async Task<BlobContainerClient> GetContainerAsync(string container, CancellationToken ct)
    {
        var containerClient = _client.GetBlobContainerClient(container);
        if (_ensuredContainers.ContainsKey(container))
        {
            return containerClient;
        }

        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        _ensuredContainers[container] = true;
        return containerClient;
    }

    private static string BlobName(string id) => $"{id}.json";

    public async Task UpsertAsync<T>(string container, string id, T entity, CancellationToken ct = default)
    {
        var containerClient = await GetContainerAsync(container, ct);
        var blob = containerClient.GetBlobClient(BlobName(id));

        var bytes = JsonSerializer.SerializeToUtf8Bytes(entity, JsonOptions);
        using var stream = new MemoryStream(bytes);
        await blob.UploadAsync(
            stream,
            new BlobHttpHeaders { ContentType = "application/json" },
            cancellationToken: ct);
    }

    public async Task<T?> GetAsync<T>(string container, string id, CancellationToken ct = default)
    {
        var containerClient = await GetContainerAsync(container, ct);
        var blob = containerClient.GetBlobClient(BlobName(id));

        try
        {
            var response = await blob.DownloadContentAsync(ct);
            return response.Value.Content.ToObjectFromJson<T>(JsonOptions);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return default;
        }
    }

    public async Task<IReadOnlyList<T>> ListAsync<T>(string container, CancellationToken ct = default)
    {
        var containerClient = await GetContainerAsync(container, ct);
        var results = new List<T>();

        await foreach (var blobItem in containerClient.GetBlobsAsync(cancellationToken: ct))
        {
            var blob = containerClient.GetBlobClient(blobItem.Name);
            var response = await blob.DownloadContentAsync(ct);
            var entity = response.Value.Content.ToObjectFromJson<T>(JsonOptions);
            if (entity is not null)
            {
                results.Add(entity);
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<T>> ListByPrefixAsync<T>(string container, string prefix, CancellationToken ct = default)
    {
        var containerClient = await GetContainerAsync(container, ct);
        var results = new List<T>();

        await foreach (var blobItem in containerClient.GetBlobsAsync(
                           traits: BlobTraits.None, states: BlobStates.None, prefix: prefix, cancellationToken: ct))
        {
            var blob = containerClient.GetBlobClient(blobItem.Name);
            var response = await blob.DownloadContentAsync(ct);
            var entity = response.Value.Content.ToObjectFromJson<T>(JsonOptions);
            if (entity is not null)
            {
                results.Add(entity);
            }
        }

        return results;
    }

    public async Task<bool> DeleteAsync(string container, string id, CancellationToken ct = default)
    {
        var containerClient = await GetContainerAsync(container, ct);
        var blob = containerClient.GetBlobClient(BlobName(id));
        var response = await blob.DeleteIfExistsAsync(cancellationToken: ct);
        return response.Value;
    }

    public async Task<bool> ExistsAsync(string container, string id, CancellationToken ct = default)
    {
        var containerClient = await GetContainerAsync(container, ct);
        var blob = containerClient.GetBlobClient(BlobName(id));
        var response = await blob.ExistsAsync(ct);
        return response.Value;
    }
}
