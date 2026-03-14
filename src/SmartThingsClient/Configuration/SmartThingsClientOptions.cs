namespace SmartThingsClient.Configuration;

public sealed record SmartThingsClientOptions
{
    public static SmartThingsClientOptions Default { get; } = new();

    public Uri BaseAddress { get; init; } = new("https://api.smartthings.com/v1/");

    public string UserAgent { get; init; } = "SmartThingsMxConsole/0.1";
}
