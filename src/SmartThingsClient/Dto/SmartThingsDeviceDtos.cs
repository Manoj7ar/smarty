using System.Text.Json.Serialization;

namespace SmartThingsClient.Dto;

internal sealed record SmartThingsListResponse<T>
{
    [JsonPropertyName("items")]
    public List<T>? Items { get; init; }
}

internal sealed record SmartThingsSceneDto
{
    [JsonPropertyName("sceneId")]
    public string? SceneId { get; init; }

    [JsonPropertyName("sceneName")]
    public string? SceneName { get; init; }

    [JsonPropertyName("locationId")]
    public string? LocationId { get; init; }
}

internal sealed record SmartThingsDeviceDto
{
    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("label")]
    public string? Label { get; init; }

    [JsonPropertyName("type")]
    public string? DeviceTypeName { get; init; }

    [JsonPropertyName("locationId")]
    public string? LocationId { get; init; }

    [JsonPropertyName("roomId")]
    public string? RoomId { get; init; }

    [JsonPropertyName("manufacturerName")]
    public string? ManufacturerName { get; init; }

    [JsonPropertyName("presentationId")]
    public string? PresentationId { get; init; }

    [JsonPropertyName("components")]
    public List<SmartThingsComponentDto>? Components { get; init; }
}

internal sealed record SmartThingsComponentDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("capabilities")]
    public List<SmartThingsCapabilityDto>? Capabilities { get; init; }
}

internal sealed record SmartThingsCapabilityDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("version")]
    public int Version { get; init; }
}
