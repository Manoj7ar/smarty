using System.Globalization;

namespace SmartThingsMxConsole.Core.Models;

public enum DeviceAvailability
{
    Unknown,
    Online,
    Offline,
}

public sealed record DeviceStateValue(
    string ComponentId,
    string CapabilityId,
    string AttributeName,
    string? Value,
    string? Unit = null,
    DateTimeOffset? Timestamp = null,
    string? RawJson = null);

public sealed record DeviceState(
    string DeviceId,
    DeviceAvailability Availability,
    DateTimeOffset LastUpdatedUtc,
    IReadOnlyList<DeviceStateValue>? Values = null,
    bool IsStale = false,
    string? ErrorMessage = null)
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public IReadOnlyList<DeviceStateValue> Values { get; init; } = Values ?? Array.Empty<DeviceStateValue>();

    public DeviceStateValue? FindValue(string capabilityId, string attributeName, string componentId = "main") =>
        this.Values.FirstOrDefault(value =>
            Comparer.Equals(value.CapabilityId, capabilityId) &&
            Comparer.Equals(value.AttributeName, attributeName) &&
            Comparer.Equals(value.ComponentId, componentId));

    public IEnumerable<DeviceStateValue> GetValuesForCapability(string capabilityId, string? componentId = null) =>
        this.Values.Where(value =>
            Comparer.Equals(value.CapabilityId, capabilityId) &&
            (componentId is null || Comparer.Equals(value.ComponentId, componentId)));

    public bool TryGetString(string capabilityId, string attributeName, out string? value, string componentId = "main")
    {
        value = this.FindValue(capabilityId, attributeName, componentId)?.Value;
        return !string.IsNullOrWhiteSpace(value);
    }

    public bool TryGetDouble(string capabilityId, string attributeName, out double value, string componentId = "main")
    {
        value = default;
        if (!this.TryGetString(capabilityId, attributeName, out var stringValue, componentId))
        {
            return false;
        }

        return double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    public DeviceState MarkStale(DateTimeOffset now, string? errorMessage = null) => this with
    {
        IsStale = true,
        ErrorMessage = errorMessage ?? this.ErrorMessage,
        LastUpdatedUtc = this.LastUpdatedUtc == default ? now : this.LastUpdatedUtc,
    };
}
