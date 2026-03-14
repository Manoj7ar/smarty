namespace SmartThingsMxConsole.Core.Models;

public sealed record DeviceCommandRequest(
    string ComponentId,
    string CapabilityId,
    string Command,
    IReadOnlyList<object?>? Arguments = null)
{
    public IReadOnlyList<object?> Arguments { get; init; } = Arguments ?? Array.Empty<object?>();
}
