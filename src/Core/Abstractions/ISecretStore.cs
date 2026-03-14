namespace SmartThingsMxConsole.Core.Abstractions;

public interface ISecretStore
{
    Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default);

    Task SaveSecretAsync(string name, string value, CancellationToken cancellationToken = default);

    Task DeleteSecretAsync(string name, CancellationToken cancellationToken = default);
}
