using SmartThingsClient.Internal;
using SmartThingsMxConsole.Core.Abstractions;
using SmartThingsMxConsole.Core.Exceptions;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsClient.Auth;

public sealed class OAuthTokenProvider : IAuthProvider, IBearerTokenProvider
{
    public Task<AuthConfig> GetAuthConfigAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(AuthConfig.SetupRequired("OAuth"));

    public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        throw new SmartThingsAuthException("OAuth is not implemented in this MVP.");
}
