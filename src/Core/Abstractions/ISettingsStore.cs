using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Core.Abstractions;

public interface ISettingsStore
{
    Task<LocalSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(LocalSettings settings, CancellationToken cancellationToken = default);
}
