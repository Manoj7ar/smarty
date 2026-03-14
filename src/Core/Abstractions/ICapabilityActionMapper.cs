using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Core.Abstractions;

public interface ICapabilityActionMapper
{
    IReadOnlyList<ConsoleBinding> MapBindings(Device device, DeviceState? state = null);

    DeviceCategory Classify(Device device);
}
