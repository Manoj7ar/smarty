namespace SmartThingsMxConsole.Plugin;

internal static class SmartThingsPluginEvents
{
    public static event Action? MetadataChanged;

    public static event Action? DisplayStateChanged;

    public static void RaiseMetadataChanged() => MetadataChanged?.Invoke();

    public static void RaiseDisplayStateChanged() => DisplayStateChanged?.Invoke();
}
