using SmartThingsMxConsole.Core.Abstractions;
using SmartThingsMxConsole.Core.Exceptions;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Core.Services;

public sealed class SceneService
{
    private readonly ISmartThingsClient _smartThingsClient;

    public SceneService(ISmartThingsClient smartThingsClient)
    {
        this._smartThingsClient = smartThingsClient;
    }

    public Task<IReadOnlyList<Scene>> ListScenesAsync(CancellationToken cancellationToken = default) =>
        this._smartThingsClient.ListScenesAsync(cancellationToken);

    public async Task<CommandExecutionResult> ExecuteSceneAsync(string sceneId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sceneId))
        {
            return CommandExecutionResult.Fail("Scene ID is required.", FailureKind.Validation);
        }

        try
        {
            await this._smartThingsClient.ExecuteSceneAsync(sceneId, cancellationToken).ConfigureAwait(false);
            return CommandExecutionResult.Ok("Scene executed.");
        }
        catch (SmartThingsAuthException)
        {
            return CommandExecutionResult.Fail("SmartThings authentication failed.", FailureKind.Auth);
        }
        catch (SmartThingsRateLimitException)
        {
            return CommandExecutionResult.Fail("SmartThings rate limit exceeded.", FailureKind.RateLimited);
        }
        catch (SmartThingsTransientException)
        {
            return CommandExecutionResult.Fail("SmartThings is temporarily unavailable.", FailureKind.Transient);
        }
        catch (SmartThingsException exception)
        {
            return CommandExecutionResult.Fail(exception.Message, FailureKind.Unknown);
        }
    }
}
