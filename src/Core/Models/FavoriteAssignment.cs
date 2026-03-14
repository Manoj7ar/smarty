namespace SmartThingsMxConsole.Core.Models;

public sealed record FavoriteAssignment(
    string EntityId,
    ConsoleBindingKind Kind,
    string DisplayName,
    DeviceCategory Category,
    ConsoleBinding Binding,
    DateTimeOffset SavedAtUtc);
