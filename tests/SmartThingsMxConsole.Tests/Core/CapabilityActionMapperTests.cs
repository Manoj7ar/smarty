using SmartThingsMxConsole.Core.Models;
using SmartThingsMxConsole.Core.Services;

namespace SmartThingsMxConsole.Tests.Core;

public sealed class CapabilityActionMapperTests
{
    private readonly CapabilityActionMapper _mapper = new();

    [Fact]
    public void MapBindings_ForSwitchAndLevel_AddsToggleAndAdjustment()
    {
        var device = new Device(
            "device-1",
            "Lamp",
            "Living Room Lamp",
            Capabilities:
            [
                new CapabilitySummary("switch", Commands: ["on", "off"]),
                new CapabilitySummary("switchLevel", Commands: ["setLevel"]),
            ]);

        var bindings = this._mapper.MapBindings(device);

        Assert.Equal(2, bindings.Count);
        Assert.Contains(bindings, binding => binding.ControlKind == DeviceControlKind.TogglePower);
        Assert.Contains(bindings, binding => binding.ControlKind == DeviceControlKind.SetLevel);
        Assert.All(bindings, binding => Assert.Equal(DeviceCategory.Lighting, binding.Category));
    }

    [Fact]
    public void MapBindings_ForLaundryDevice_AddsStatusBinding()
    {
        var device = new Device(
            "device-2",
            "Washer",
            "Washer",
            Capabilities:
            [
                new CapabilitySummary("custom.washerOperatingState", Attributes: ["machineState", "remainingTime"]),
            ]);

        var bindings = this._mapper.MapBindings(device);

        var binding = Assert.Single(bindings);
        Assert.True(binding.IsReadOnly);
        Assert.Equal(ConsoleBindingKind.DeviceStatus, binding.Kind);
        Assert.Equal(DeviceCategory.Laundry, binding.Category);
    }

    [Fact]
    public void MapBindings_ForMediaDevice_AddsPlaybackBindings()
    {
        var device = new Device(
            "device-3",
            "TV",
            "TV",
            Capabilities:
            [
                new CapabilitySummary("switch", Commands: ["on", "off"]),
                new CapabilitySummary("mediaPlayback", Commands: ["play", "pause", "stop"]),
                new CapabilitySummary("mediaTrackControl", Commands: ["nextTrack", "previousTrack"]),
            ]);

        var bindings = this._mapper.MapBindings(device);

        Assert.Contains(bindings, binding => binding.ControlKind == DeviceControlKind.TogglePower);
        Assert.Contains(bindings, binding => binding.ControlKind == DeviceControlKind.TogglePlayback);
        Assert.Contains(bindings, binding => binding.ControlKind == DeviceControlKind.NextTrack);
        Assert.All(bindings, binding => Assert.Equal(DeviceCategory.Media, binding.Category));
    }
}
