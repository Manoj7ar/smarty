namespace SmartThingsMxConsole.Core.Models;

public sealed record Device(
    string Id,
    string Name,
    string Label,
    string? LocationId = null,
    string? RoomId = null,
    string? RoomName = null,
    string? DeviceTypeId = null,
    IReadOnlyList<CapabilitySummary>? Capabilities = null)
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public IReadOnlyList<CapabilitySummary> Capabilities { get; init; } = Capabilities ?? Array.Empty<CapabilitySummary>();

    public string DisplayName => !string.IsNullOrWhiteSpace(Label) ? Label : Name;

    public bool HasCapability(string capabilityId) => this.Capabilities.Any(capability => Comparer.Equals(capability.CapabilityId, capabilityId));

    public CapabilitySummary? GetCapability(string capabilityId) =>
        this.Capabilities.FirstOrDefault(capability => Comparer.Equals(capability.CapabilityId, capabilityId));
}
