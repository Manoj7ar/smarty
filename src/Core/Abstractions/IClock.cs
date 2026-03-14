namespace SmartThingsMxConsole.Core.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
