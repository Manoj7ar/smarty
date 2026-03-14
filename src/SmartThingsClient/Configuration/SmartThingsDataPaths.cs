namespace SmartThingsClient.Configuration;

public sealed record SmartThingsDataPaths(string RootDirectory, string SettingsPath, string CachePath, string SecretsPath)
{
    public const string DefaultPersonalAccessTokenSecretName = "smartthings-pat";

    public static SmartThingsDataPaths CreateDefault()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartThingsMxConsole");

        return new SmartThingsDataPaths(
            root,
            Path.Combine(root, "settings.json"),
            Path.Combine(root, "cache.json"),
            Path.Combine(root, "secrets.json"));
    }
}
