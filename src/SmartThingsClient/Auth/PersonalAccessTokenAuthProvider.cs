using SmartThingsClient.Configuration;
using SmartThingsClient.Internal;
using SmartThingsMxConsole.Core.Abstractions;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsClient.Auth;

public sealed class PersonalAccessTokenAuthProvider(
    ISecretStore secretStore,
    string secretName = SmartThingsDataPaths.DefaultPersonalAccessTokenSecretName) : IAuthProvider, IBearerTokenProvider
{
    public async Task<AuthConfig> GetAuthConfigAsync(CancellationToken cancellationToken = default)
    {
        var token = await secretStore.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(token)
            ? AuthConfig.SetupRequired("PAT")
            : AuthConfig.Authenticated("PAT", "Personal Access Token", DateTimeOffset.UtcNow);
    }

    public async ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        => await secretStore.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
}
