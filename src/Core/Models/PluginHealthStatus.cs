namespace SmartThingsMxConsole.Core.Models;

public enum PluginHealthState
{
    SetupRequired,
    Connected,
    InvalidAuth,
    Degraded,
    Error,
}

public sealed record PluginHealthStatus(
    PluginHealthState State,
    string Message,
    string? SupportUrl = null,
    string? SupportUrlTitle = null);
