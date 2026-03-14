using SmartThingsMxConsole.Core.Models;
using SmartThingsMxConsole.Core.Services;

namespace SmartThingsMxConsole.Tests.Core;

public sealed class DeviceStateFormatterTests
{
    private readonly DeviceStateFormatter _formatter = new();

    [Fact]
    public void FormatDeviceLabel_ForSwitchDevice_ReturnsReadableState()
    {
        var device = new Device("device-1", "Lamp", "Living Room Lamp");
        var state = new DeviceState(
            "device-1",
            DeviceAvailability.Online,
            DateTimeOffset.Parse("2026-03-11T12:00:00Z"),
            [new DeviceStateValue("main", "switch", "switch", "on")]);

        var label = _formatter.FormatDeviceLabel(device, state);

        Assert.Equal("Living Room Lamp · On", label);
    }

    [Fact]
    public void FormatDeviceLabel_ForLevelDevice_PrefersPercentage()
    {
        var device = new Device("device-1", "Fan", "Bedroom Fan");
        var state = new DeviceState(
            "device-1",
            DeviceAvailability.Online,
            DateTimeOffset.Parse("2026-03-11T12:00:00Z"),
            [new DeviceStateValue("main", "switchLevel", "level", "62")]);

        var label = _formatter.FormatDeviceLabel(device, state);

        Assert.Equal("Bedroom Fan · 62%", label);
    }

    [Fact]
    public void FormatDeviceLabel_ForMediaPowerOff_ReturnsOff()
    {
        var device = new Device("device-1", "TV", "TV");
        var state = new DeviceState(
            "device-1",
            DeviceAvailability.Online,
            DateTimeOffset.Parse("2026-03-11T12:00:00Z"),
            [new DeviceStateValue("main", "switch", "switch", "off")]);

        var label = _formatter.FormatDeviceLabel(device, state);

        Assert.Equal("TV · Off", label);
    }

    [Fact]
    public void FormatDeviceLabel_ForLaundryTimer_ReturnsRemainingMinutes()
    {
        var now = DateTimeOffset.Parse("2026-03-11T12:00:00Z");
        var device = new Device("device-1", "Washer", "Washer");
        var state = new DeviceState(
            "device-1",
            DeviceAvailability.Online,
            now,
            [new DeviceStateValue("main", "custom.washerOperatingState", "completionTime", now.AddMinutes(18).ToString("O"))]);

        var label = _formatter.FormatDeviceLabel(device, state);

        Assert.Equal("Washer · 18m left", label);
    }
}
