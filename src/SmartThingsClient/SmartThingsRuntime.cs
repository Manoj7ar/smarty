using SmartThingsClient.Auth;
using SmartThingsClient.Configuration;
using SmartThingsClient.Internal;
using SmartThingsClient.Storage;
using SmartThingsMxConsole.Core.Abstractions;

namespace SmartThingsClient;

public sealed class SmartThingsRuntime : IDisposable
{
    public SmartThingsRuntime(
        SmartThingsDataPaths dataPaths,
        HttpClient httpClient,
        ISmartThingsClient client,
        IAuthProvider authProvider,
        ISettingsStore settingsStore,
        IMetadataCache metadataCache,
        ISecretStore secretStore)
    {
        DataPaths = dataPaths;
        HttpClient = httpClient;
        Client = client;
        AuthProvider = authProvider;
        SettingsStore = settingsStore;
        MetadataCache = metadataCache;
        SecretStore = secretStore;
    }

    public SmartThingsDataPaths DataPaths { get; }

    public HttpClient HttpClient { get; }

    public ISmartThingsClient Client { get; }

    public IAuthProvider AuthProvider { get; }

    public ISettingsStore SettingsStore { get; }

    public IMetadataCache MetadataCache { get; }

    public ISecretStore SecretStore { get; }

    public void Dispose() => HttpClient.Dispose();
}

public static class SmartThingsRuntimeFactory
{
    public static SmartThingsRuntime CreateDefault(SmartThingsClientOptions? options = null, SmartThingsDataPaths? dataPaths = null)
    {
        var resolvedPaths = dataPaths ?? SmartThingsDataPaths.CreateDefault();
        var settingsStore = SmartThingsStorageFactory.CreateSettingsStore(resolvedPaths);
        var metadataCache = SmartThingsStorageFactory.CreateMetadataCache(resolvedPaths);
        var secretStore = SmartThingsStorageFactory.CreateSecretStore(resolvedPaths);
        var authProvider = new PersonalAccessTokenAuthProvider(secretStore);
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        var client = new SmartThingsHttpClient(httpClient, (IBearerTokenProvider)authProvider, options);

        return new SmartThingsRuntime(
            resolvedPaths,
            httpClient,
            client,
            authProvider,
            settingsStore,
            metadataCache,
            secretStore);
    }
}
