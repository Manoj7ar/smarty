using System.Text.Json;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsClient.Internal;

internal static class DeviceStateJsonParser
{
    public static DeviceState ToDeviceState(this JsonElement root, string deviceId)
    {
        if (!JsonElementExtensions.TryGetProperty(root, "components", out var componentsElement) ||
            componentsElement.ValueKind != JsonValueKind.Object)
        {
            return new DeviceState(deviceId, DeviceAvailability.Unknown, DateTimeOffset.UtcNow);
        }

        var values = new List<DeviceStateValue>();

        foreach (var componentProperty in componentsElement.EnumerateObject())
        {
            if (componentProperty.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var capabilityProperty in componentProperty.Value.EnumerateObject())
            {
                if (!JsonElementExtensions.TryGetProperty(capabilityProperty.Value, "attributes", out var attributesElement) ||
                    attributesElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var attributeProperty in attributesElement.EnumerateObject())
                {
                    if (attributeProperty.Value.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var stringValue = JsonElementExtensions.TryGetProperty(attributeProperty.Value, "value", out var valueElement)
                        ? ConvertToString(valueElement)
                        : null;
                    var rawJson = attributeProperty.Value.GetRawText();
                    var unit = JsonElementExtensions.TryGetProperty(attributeProperty.Value, "unit", out var unitElement) && unitElement.ValueKind == JsonValueKind.String
                        ? unitElement.GetString()
                        : null;
                    var timestamp = JsonElementExtensions.TryGetProperty(attributeProperty.Value, "timestamp", out var timestampElement) &&
                                    timestampElement.ValueKind == JsonValueKind.String &&
                                    DateTimeOffset.TryParse(timestampElement.GetString(), out var parsedTimestamp)
                        ? parsedTimestamp
                        : (DateTimeOffset?)null;

                    values.Add(new DeviceStateValue(
                        componentProperty.Name,
                        capabilityProperty.Name,
                        attributeProperty.Name,
                        stringValue,
                        unit,
                        timestamp,
                        rawJson));
                }
            }
        }

        var availability = ResolveAvailability(values);
        return new DeviceState(deviceId, availability, DateTimeOffset.UtcNow, values);
    }

    private static DeviceAvailability ResolveAvailability(IReadOnlyList<DeviceStateValue> values)
    {
        var statusValue = values.FirstOrDefault(value =>
            string.Equals(value.AttributeName, "deviceStatus", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value.AttributeName, "DeviceWatch-DeviceStatus", StringComparison.OrdinalIgnoreCase));

        return statusValue?.Value?.ToLowerInvariant() switch
        {
            "online" => DeviceAvailability.Online,
            "offline" => DeviceAvailability.Offline,
            _ => DeviceAvailability.Unknown
        };
    }

    private static string? ConvertToString(JsonElement valueElement) =>
        valueElement.ValueKind switch
        {
            JsonValueKind.String => valueElement.GetString(),
            JsonValueKind.Number => valueElement.GetRawText(),
            JsonValueKind.True => bool.TrueString.ToLowerInvariant(),
            JsonValueKind.False => bool.FalseString.ToLowerInvariant(),
            JsonValueKind.Object or JsonValueKind.Array => valueElement.GetRawText(),
            _ => null
        };
}
