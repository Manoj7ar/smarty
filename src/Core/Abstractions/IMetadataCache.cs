using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Core.Abstractions;

public interface IMetadataCache
{
    Task<MetadataSnapshot?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(MetadataSnapshot snapshot, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
