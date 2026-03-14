namespace SmartThingsMxConsole.Core.Models;

public sealed record ProfileActionItem(
    string DisplayName,
    string Description,
    ConsoleBinding Binding,
    bool IsPlaceholder = false);

public sealed record ProfileSection(
    string Key,
    string DisplayName,
    IReadOnlyList<ProfileActionItem>? Items = null)
{
    public IReadOnlyList<ProfileActionItem> Items { get; init; } = Items ?? Array.Empty<ProfileActionItem>();
}
