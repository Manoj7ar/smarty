using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Core.Abstractions;

public interface IDeviceStateFormatter
{
    string FormatDeviceLabel(Device device, DeviceState? state, ConsoleBinding? binding = null);

    string FormatSceneLabel(Scene scene, bool isReady = true);

    string FormatPluginHealth(PluginHealthStatus status);
}
