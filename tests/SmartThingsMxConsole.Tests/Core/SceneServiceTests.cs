using SmartThingsMxConsole.Core.Exceptions;
using SmartThingsMxConsole.Core.Models;
using SmartThingsMxConsole.Core.Services;

namespace SmartThingsMxConsole.Tests.Core;

public sealed class SceneServiceTests
{
    [Fact]
    public async Task ExecuteSceneAsync_ReturnsSuccessWhenClientSucceeds()
    {
        var client = new FakeSmartThingsClient();
        var service = new SceneService(client);

        var result = await service.ExecuteSceneAsync("scene-1");

        Assert.True(result.Success);
        Assert.Equal(["scene-1"], client.SceneExecutions);
    }

    [Fact]
    public async Task ExecuteSceneAsync_ReturnsAuthFailureWhenTokenIsInvalid()
    {
        var client = new FakeSmartThingsClient
        {
            ExecuteSceneException = new SmartThingsAuthException(),
        };
        var service = new SceneService(client);

        var result = await service.ExecuteSceneAsync("scene-1");

        Assert.False(result.Success);
        Assert.Equal(FailureKind.Auth, result.FailureKind);
    }
}
