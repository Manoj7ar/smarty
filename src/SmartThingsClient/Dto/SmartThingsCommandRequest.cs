using System.Text.Json.Serialization;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsClient.Dto;

internal sealed record SmartThingsCommandRequest
{
    [JsonPropertyName("commands")]
    public required List<SmartThingsCommandDto> Commands { get; init; }

    public static SmartThingsCommandRequest FromCommands(IEnumerable<DeviceCommandRequest> commands) =>
        new()
        {
            Commands = commands.Select(command => new SmartThingsCommandDto
            {
                Component = string.IsNullOrWhiteSpace(command.ComponentId) ? "main" : command.ComponentId,
                Capability = command.CapabilityId,
                Command = command.Command,
                Arguments = command.Arguments?.ToList() ?? []
            }).ToList()
        };
}

internal sealed record SmartThingsCommandDto
{
    [JsonPropertyName("component")]
    public required string Component { get; init; }

    [JsonPropertyName("capability")]
    public required string Capability { get; init; }

    [JsonPropertyName("command")]
    public required string Command { get; init; }

    [JsonPropertyName("arguments")]
    public required List<object?> Arguments { get; init; }
}
