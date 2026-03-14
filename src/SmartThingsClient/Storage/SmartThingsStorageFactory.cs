using SmartThingsClient.Configuration;
using SmartThingsMxConsole.Core.Abstractions;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsClient.Storage;

public static class SmartThingsStorageFactory
{
    public static JsonFileStore<LocalSettings> CreateSettingsBackingStore(SmartThingsDataPaths? dataPaths = null)
    {
        var paths = dataPaths ?? SmartThingsDataPaths.CreateDefault();
        return new JsonFileStore<LocalSettings>(paths.SettingsPath, () => LocalSettings.Default);
    }

    public static ISettingsStore CreateSettingsStore(SmartThingsDataPaths? dataPaths = null)
    {
        var paths = dataPaths ?? SmartThingsDataPaths.CreateDefault();
        return new LocalSettingsStore(CreateSettingsBackingStore(paths));
    }

    public static IMetadataCache CreateMetadataCache(SmartThingsDataPaths? dataPaths = null)
    {
        var paths = dataPaths ?? SmartThingsDataPaths.CreateDefault();
        return new MetadataCacheStore(new JsonFileStore<MetadataSnapshot>(paths.CachePath, () => new MetadataSnapshot()), paths.CachePath);
    }

    public static ISecretStore CreateSecretStore(SmartThingsDataPaths? dataPaths = null)
    {
        var paths = dataPaths ?? SmartThingsDataPaths.CreateDefault();
        return new JsonSecretStore(paths.SecretsPath);
    }
}
