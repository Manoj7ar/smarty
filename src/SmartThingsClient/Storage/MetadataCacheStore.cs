using SmartThingsMxConsole.Core.Abstractions;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsClient.Storage;

public sealed class MetadataCacheStore(JsonFileStore<MetadataSnapshot> backingStore, string path) : IMetadataCache
{
    public async Task<MetadataSnapshot?> LoadAsync(CancellationToken cancellationToken = default) =>
        await backingStore.LoadAsync(cancellationToken).ConfigureAwait(false);

    public Task SaveAsync(MetadataSnapshot snapshot, CancellationToken cancellationToken = default) =>
        backingStore.SaveAsync(snapshot, cancellationToken);

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
