using SmartThingsMxConsole.Core.Exceptions;
using SmartThingsMxConsole.Core.Models;
using SmartThingsMxConsole.Core.Services;

namespace SmartThingsMxConsole.Tests.Core;

public sealed class PollingCoordinatorTests
{
    [Fact]
    public async Task UpdateWatchedBindingsAsync_DeduplicatesDeviceIds()
    {
        var client = new FakeSmartThingsClient();
        var coordinator = new PollingCoordinator(client, new FakeClock());

        await coordinator.UpdateWatchedBindingsAsync(
        [
            new ConsoleBinding(ConsoleBindingKind.DeviceCommand, "device-1", "Device 1"),
            new ConsoleBinding(ConsoleBindingKind.DeviceStatus, "device-1", "Device 1"),
            new ConsoleBinding(ConsoleBindingKind.DeviceAdjustment, "device-2", "Device 2"),
        ]);

        Assert.Equal(2, coordinator.WatchedDeviceIds.Count);
        Assert.Contains("device-1", coordinator.WatchedDeviceIds);
        Assert.Contains("device-2", coordinator.WatchedDeviceIds);
    }

    [Fact]
    public async Task RefreshNowAsync_RequestsEachWatchedDeviceAndStoresLatestState()
    {
        var now = DateTimeOffset.Parse("2026-03-11T12:00:00Z");
        var client = new FakeSmartThingsClient();
        client.SetState(new DeviceState("device-1", DeviceAvailability.Online, now, [new DeviceStateValue("main", "switch", "switch", "on")]));

        var coordinator = new PollingCoordinator(client, new FakeClock { UtcNow = now });
        await coordinator.UpdateWatchedBindingsAsync([new ConsoleBinding(ConsoleBindingKind.DeviceCommand, "device-1", "Lamp")]);

        await coordinator.RefreshNowAsync();

        Assert.Equal(["device-1"], client.RequestedStatuses);
        Assert.True(coordinator.TryGetLatestState("device-1", out var state));
        Assert.NotNull(state);
        Assert.False(state!.IsStale);
    }

    [Fact]
    public async Task RefreshNowAsync_KeepsLastKnownStateAndMarksItStaleAfterRepeatedTransientFailure()
    {
        var now = DateTimeOffset.Parse("2026-03-11T12:00:00Z");
        var clock = new FakeClock { UtcNow = now };
        var client = new FakeSmartThingsClient();
        client.SetState(new DeviceState("device-1", DeviceAvailability.Online, now, [new DeviceStateValue("main", "switch", "switch", "on")]));

        var coordinator = new PollingCoordinator(client, clock, TimeSpan.FromSeconds(15));
        await coordinator.UpdateWatchedBindingsAsync([new ConsoleBinding(ConsoleBindingKind.DeviceCommand, "device-1", "Lamp")]);
        await coordinator.RefreshNowAsync();

        client.StatusException = new SmartThingsTransientException("Network failure");
        clock.UtcNow = now.AddSeconds(45);

        await coordinator.RefreshNowAsync();

        Assert.True(coordinator.TryGetLatestState("device-1", out var staleState));
        Assert.NotNull(staleState);
        Assert.True(staleState!.IsStale);
        Assert.Equal("Network failure", staleState.ErrorMessage);
    }
}
