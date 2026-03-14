using System.Collections.Concurrent;
using SmartThingsMxConsole.Core.Abstractions;
using SmartThingsMxConsole.Core.Exceptions;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Core.Services;

public sealed class PollingCoordinator : IPollingCoordinator
{
    private readonly ISmartThingsClient _smartThingsClient;
    private readonly IClock _clock;
    private readonly SemaphoreSlim _refreshGate = new(4, 4);
    private readonly ConcurrentDictionary<string, byte> _watchedDeviceIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DeviceState> _latestStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _stateMutationGate = new(1, 1);
    private CancellationTokenSource? _loopCancellationTokenSource;
    private Task? _loopTask;
    private TimeSpan _refreshInterval;

    public PollingCoordinator(ISmartThingsClient smartThingsClient, IClock clock, TimeSpan? refreshInterval = null)
    {
        this._smartThingsClient = smartThingsClient;
        this._clock = clock;
        this._refreshInterval = refreshInterval.GetValueOrDefault(TimeSpan.FromSeconds(15));
    }

    public event EventHandler<DeviceStateChangedEventArgs>? DeviceStateChanged;

    public TimeSpan RefreshInterval => this._refreshInterval;

    public IReadOnlyCollection<string> WatchedDeviceIds => this._watchedDeviceIds.Keys.ToArray();

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await this._stateMutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (this._loopTask is not null)
            {
                return;
            }

            this._loopCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this._loopTask = Task.Run(() => this.RunLoopAsync(this._loopCancellationTokenSource.Token), CancellationToken.None);
        }
        finally
        {
            this._stateMutationGate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Task? loopTask;

        await this._stateMutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (this._loopCancellationTokenSource is null)
            {
                return;
            }

            this._loopCancellationTokenSource.Cancel();
            loopTask = this._loopTask;
            this._loopCancellationTokenSource.Dispose();
            this._loopCancellationTokenSource = null;
            this._loopTask = null;
        }
        finally
        {
            this._stateMutationGate.Release();
        }

        if (loopTask is not null)
        {
            try
            {
                await loopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    public async Task UpdateWatchedBindingsAsync(IEnumerable<ConsoleBinding> bindings, CancellationToken cancellationToken = default)
    {
        await this._stateMutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            this._watchedDeviceIds.Clear();
            foreach (var binding in bindings.Where(binding => binding.Kind is ConsoleBindingKind.DeviceCommand or ConsoleBindingKind.DeviceAdjustment or ConsoleBindingKind.DeviceStatus))
            {
                this._watchedDeviceIds.TryAdd(binding.EntityId, 0);
            }
        }
        finally
        {
            this._stateMutationGate.Release();
        }
    }

    public async Task RefreshNowAsync(CancellationToken cancellationToken = default)
    {
        var now = this._clock.UtcNow;
        var tasks = this._watchedDeviceIds.Keys.Select(deviceId => RefreshDeviceAsync(deviceId, now, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public bool TryGetLatestState(string deviceId, out DeviceState? state)
    {
        var hasState = this._latestStates.TryGetValue(deviceId, out var latest);
        state = latest;
        return hasState;
    }

    public void SetRefreshInterval(TimeSpan refreshInterval)
    {
        if (refreshInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(refreshInterval));
        }

        this._refreshInterval = refreshInterval;
    }

    public async ValueTask DisposeAsync()
    {
        await this.StopAsync().ConfigureAwait(false);
        this._refreshGate.Dispose();
        this._stateMutationGate.Dispose();
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await this.RefreshNowAsync(cancellationToken).ConfigureAwait(false);
            await Task.Delay(this._refreshInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshDeviceAsync(string deviceId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await this._refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await this._smartThingsClient.GetDeviceStatusAsync(deviceId, cancellationToken).ConfigureAwait(false);
            var updatedState = state with { IsStale = false, ErrorMessage = null };
            this._latestStates[deviceId] = updatedState;
            this.DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs(updatedState));
        }
        catch (SmartThingsTransientException exception)
        {
            MarkExistingStateAsStale(deviceId, now, exception.Message);
        }
        catch (SmartThingsRateLimitException exception)
        {
            MarkExistingStateAsStale(deviceId, now, exception.Message);
        }
        catch (SmartThingsAuthException exception)
        {
            MarkExistingStateAsStale(deviceId, now, exception.Message);
        }
    }

    private void MarkExistingStateAsStale(string deviceId, DateTimeOffset now, string message)
    {
        if (!this._latestStates.TryGetValue(deviceId, out var currentState))
        {
            return;
        }

        if (now - currentState.LastUpdatedUtc < this._refreshInterval + this._refreshInterval)
        {
            return;
        }

        var staleState = currentState.MarkStale(now, message);
        this._latestStates[deviceId] = staleState;
        this.DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs(staleState));
    }
}
