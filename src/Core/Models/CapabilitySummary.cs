namespace SmartThingsMxConsole.Core.Models;

public sealed record CapabilitySummary(
    string CapabilityId,
    int Version = 1,
    IReadOnlyList<string>? Commands = null,
    IReadOnlyList<string>? Attributes = null,
    bool IsReadable = true,
    bool IsWritable = false)
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public IReadOnlyList<string> Commands { get; init; } = Commands ?? Array.Empty<string>();

    public IReadOnlyList<string> Attributes { get; init; } = Attributes ?? Array.Empty<string>();

    public bool SupportsCommand(string command) => this.Commands.Contains(command, Comparer);

    public bool HasAttribute(string attribute) => this.Attributes.Contains(attribute, Comparer);
}
