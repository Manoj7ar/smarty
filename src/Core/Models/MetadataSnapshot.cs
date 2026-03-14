namespace SmartThingsMxConsole.Core.Models;

public sealed record MetadataSnapshot(
    IReadOnlyList<Scene>? Scenes = null,
    IReadOnlyList<Device>? Devices = null,
    IReadOnlyDictionary<string, DeviceState>? DeviceStates = null,
    DateTimeOffset? SavedAtUtc = null)
{
    public IReadOnlyList<Scene> Scenes { get; init; } = Scenes ?? Array.Empty<Scene>();

    public IReadOnlyList<Device> Devices { get; init; } = Devices ?? Array.Empty<Device>();

    public IReadOnlyDictionary<string, DeviceState> DeviceStates { get; init; } =
        DeviceStates ?? new Dictionary<string, DeviceState>(StringComparer.OrdinalIgnoreCase);
}
