namespace SmartThingsMxConsole.Core.Models;

public sealed record AuthConfig(
    string ProviderName,
    bool IsConfigured,
    bool IsAuthenticated,
    string? AccountLabel = null,
    DateTimeOffset? UpdatedAtUtc = null)
{
    public static AuthConfig SetupRequired(string providerName = "PAT") => new(providerName, false, false);

    public static AuthConfig Authenticated(string providerName, string? accountLabel = null, DateTimeOffset? updatedAtUtc = null) =>
        new(providerName, true, true, accountLabel, updatedAtUtc);
}
