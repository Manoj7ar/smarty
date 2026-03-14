namespace SmartThingsMxConsole.Core.Models;

public sealed record LocalSettings(
    TimeSpan RefreshInterval,
    IReadOnlyList<FavoriteAssignment>? Favorites = null,
    bool PollAssignedDevicesOnly = true)
{
    public IReadOnlyList<FavoriteAssignment> Favorites { get; init; } = Favorites ?? Array.Empty<FavoriteAssignment>();

    public static LocalSettings Default { get; } = new(TimeSpan.FromSeconds(15));
}
