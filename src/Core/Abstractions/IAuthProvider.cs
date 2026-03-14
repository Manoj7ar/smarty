using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Core.Abstractions;

public interface IAuthProvider
{
    Task<AuthConfig> GetAuthConfigAsync(CancellationToken cancellationToken = default);
}
