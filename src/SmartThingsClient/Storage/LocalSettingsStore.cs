using SmartThingsMxConsole.Core.Abstractions;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsClient.Storage;

public sealed class LocalSettingsStore(JsonFileStore<LocalSettings> backingStore) : ISettingsStore
{
    public Task<LocalSettings> LoadAsync(CancellationToken cancellationToken = default) =>
        backingStore.LoadAsync(cancellationToken);

    public Task SaveAsync(LocalSettings settings, CancellationToken cancellationToken = default) =>
        backingStore.SaveAsync(settings, cancellationToken);
}
