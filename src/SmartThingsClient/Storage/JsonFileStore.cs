using System.Text.Json;
using SmartThingsClient.Internal;

namespace SmartThingsClient.Storage;

public sealed class JsonFileStore<T>(string path, Func<T> defaultFactory)
{
    public async Task<T> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return defaultFactory();
        }

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        var value = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions.Default, cancellationToken).ConfigureAwait(false);
        return value ?? defaultFactory();
    }

    public async Task SaveAsync(T value, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions.Default, cancellationToken).ConfigureAwait(false);
    }
}
