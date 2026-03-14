namespace SmartThingsClient.Internal;

public interface IBearerTokenProvider
{
    ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
