using System.Runtime.Versioning;
using SmartThingsClient.Storage;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Tests.SmartThingsClient;

[SupportedOSPlatform("windows")]
public sealed class StorageTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "SmartThingsMxConsole.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task JsonFileStore_RoundTripsSettings()
    {
        var backingStore = new JsonFileStore<LocalSettings>(
            Path.Combine(_tempRoot, "settings.json"),
            () => LocalSettings.Default);
        var store = new LocalSettingsStore(backingStore);

        var settings = new LocalSettings(TimeSpan.FromSeconds(30));

        await store.SaveAsync(settings);
        var loaded = await store.LoadAsync();

        Assert.Equal(TimeSpan.FromSeconds(30), loaded.RefreshInterval);
    }

    [Fact]
    public async Task JsonSecretStore_RoundTripsSecrets()
    {
        var store = new JsonSecretStore(Path.Combine(_tempRoot, "secrets.json"));

        await store.SaveSecretAsync("smartthings-pat", "super-secret");
        var secret = await store.GetSecretAsync("smartthings-pat");
        await store.DeleteSecretAsync("smartthings-pat");
        var removed = await store.GetSecretAsync("smartthings-pat");

        Assert.Equal("super-secret", secret);
        Assert.Null(removed);
    }

    [Fact]
    public async Task JsonFileStore_RoundTripsCacheSnapshot()
    {
        var path = Path.Combine(_tempRoot, "cache.json");
        var store = new MetadataCacheStore(
            new JsonFileStore<MetadataSnapshot>(path, () => new MetadataSnapshot()),
            path);

        var snapshot = new MetadataSnapshot(
            Scenes: [new Scene("scene-1", "Relax")],
            Devices: [new Device("device-1", "Lamp", "Lamp")]);

        await store.SaveAsync(snapshot);
        var loaded = await store.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal("scene-1", Assert.Single(loaded!.Scenes).Id);
        Assert.Equal("device-1", Assert.Single(loaded.Devices).Id);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
