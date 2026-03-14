using SmartThingsMxConsole.Core.Abstractions;
using SmartThingsMxConsole.Core.Exceptions;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Tests.Core;

internal sealed class FakeSmartThingsClient : ISmartThingsClient
{
    private readonly Dictionary<string, DeviceState> _states = new(StringComparer.OrdinalIgnoreCase);

    public List<string> SceneExecutions { get; } = new();

    public List<string> RequestedStatuses { get; } = new();

    public Exception? ExecuteSceneException { get; set; }

    public Exception? StatusException { get; set; }

    public IReadOnlyList<Scene> Scenes { get; set; } = Array.Empty<Scene>();

    public IReadOnlyList<Device> Devices { get; set; } = Array.Empty<Device>();

    public Task<IReadOnlyList<Scene>> ListScenesAsync(CancellationToken cancellationToken = default) => Task.FromResult(this.Scenes);

    public Task ExecuteSceneAsync(string sceneId, CancellationToken cancellationToken = default)
    {
        this.SceneExecutions.Add(sceneId);
        if (this.ExecuteSceneException is not null)
        {
            throw this.ExecuteSceneException;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Device>> ListDevicesAsync(CancellationToken cancellationToken = default) => Task.FromResult(this.Devices);

    public Task<DeviceState> GetDeviceStatusAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        this.RequestedStatuses.Add(deviceId);

        if (this.StatusException is not null)
        {
            throw this.StatusException;
        }

        return Task.FromResult(this._states[deviceId]);
    }

    public Task SendDeviceCommandAsync(string deviceId, DeviceCommandRequest command, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public void SetState(DeviceState state) => this._states[state.DeviceId] = state;
}

internal sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.Parse("2026-03-11T12:00:00Z");
}
