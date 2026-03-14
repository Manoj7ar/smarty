using SmartThingsMxConsole.Core.Abstractions;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Core.Services;

public sealed class MetadataService
{
    private readonly ISmartThingsClient _smartThingsClient;
    private readonly IMetadataCache _metadataCache;

    public MetadataService(ISmartThingsClient smartThingsClient, IMetadataCache metadataCache)
    {
        this._smartThingsClient = smartThingsClient;
        this._metadataCache = metadataCache;
    }

    public Task<MetadataSnapshot?> LoadCachedAsync(CancellationToken cancellationToken = default) =>
        this._metadataCache.LoadAsync(cancellationToken);

    public async Task<MetadataSnapshot> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var scenes = await this._smartThingsClient.ListScenesAsync(cancellationToken).ConfigureAwait(false);
        var devices = await this._smartThingsClient.ListDevicesAsync(cancellationToken).ConfigureAwait(false);

        var snapshot = new MetadataSnapshot(
            Scenes: scenes,
            Devices: devices,
            SavedAtUtc: DateTimeOffset.UtcNow);

        await this._metadataCache.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
        return snapshot;
    }

    public Task SaveAsync(MetadataSnapshot snapshot, CancellationToken cancellationToken = default) =>
        this._metadataCache.SaveAsync(snapshot, cancellationToken);

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        this._metadataCache.ClearAsync(cancellationToken);
}
