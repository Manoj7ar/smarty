using System.Text.Json;
using SmartThingsClient.Internal;
using SmartThingsMxConsole.Core.Abstractions;

namespace SmartThingsClient.Storage;

public sealed class JsonSecretStore(string path) : ISecretStore
{
    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var payload = await LoadPayloadAsync(cancellationToken).ConfigureAwait(false);
        if (!payload.TryGetValue(name, out var encryptedValue))
        {
            return null;
        }

        return encryptedValue;
    }

    public async Task SaveSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var payload = await LoadPayloadAsync(cancellationToken).ConfigureAwait(false);
        payload[name] = value;
        await SavePayloadAsync(payload, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var payload = await LoadPayloadAsync(cancellationToken).ConfigureAwait(false);
        if (payload.Remove(name))
        {
            await SavePayloadAsync(payload, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<Dictionary<string, string>> LoadPayloadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        var payload = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, JsonOptions.Default, cancellationToken).ConfigureAwait(false);
        return payload ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private async Task SavePayloadAsync(Dictionary<string, string> payload, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        await JsonSerializer.SerializeAsync(stream, payload, JsonOptions.Default, cancellationToken).ConfigureAwait(false);
    }
}
