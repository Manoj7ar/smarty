using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Core.Abstractions;

public interface ISmartThingsClient
{
    Task<IReadOnlyList<Scene>> ListScenesAsync(CancellationToken cancellationToken = default);

    Task ExecuteSceneAsync(string sceneId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Device>> ListDevicesAsync(CancellationToken cancellationToken = default);

    Task<DeviceState> GetDeviceStatusAsync(string deviceId, CancellationToken cancellationToken = default);

    Task SendDeviceCommandAsync(string deviceId, DeviceCommandRequest command, CancellationToken cancellationToken = default);
}
