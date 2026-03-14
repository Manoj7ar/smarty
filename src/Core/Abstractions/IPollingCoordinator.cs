using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Core.Abstractions;

public interface IPollingCoordinator : IAsyncDisposable
{
    event EventHandler<DeviceStateChangedEventArgs>? DeviceStateChanged;

    TimeSpan RefreshInterval { get; }

    IReadOnlyCollection<string> WatchedDeviceIds { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task UpdateWatchedBindingsAsync(IEnumerable<ConsoleBinding> bindings, CancellationToken cancellationToken = default);

    Task RefreshNowAsync(CancellationToken cancellationToken = default);

    bool TryGetLatestState(string deviceId, out DeviceState? state);

    void SetRefreshInterval(TimeSpan refreshInterval);
}

public sealed class DeviceStateChangedEventArgs : EventArgs
{
    public DeviceStateChangedEventArgs(DeviceState state)
    {
        this.State = state;
    }

    public DeviceState State { get; }
}
