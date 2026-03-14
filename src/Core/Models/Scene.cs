namespace SmartThingsMxConsole.Core.Models;

public sealed record Scene(
    string Id,
    string Name,
    string? LocationId = null,
    bool IsPinned = false,
    string? Description = null,
    string? Icon = null)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Id : Name;
}
