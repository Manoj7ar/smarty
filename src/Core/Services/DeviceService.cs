using SmartThingsMxConsole.Core.Abstractions;
using SmartThingsMxConsole.Core.Exceptions;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Core.Services;

public sealed class DeviceService
{
    private readonly ISmartThingsClient _smartThingsClient;
    private readonly ICapabilityActionMapper _capabilityActionMapper;

    public DeviceService(ISmartThingsClient smartThingsClient, ICapabilityActionMapper capabilityActionMapper)
    {
        this._smartThingsClient = smartThingsClient;
        this._capabilityActionMapper = capabilityActionMapper;
    }

    public Task<IReadOnlyList<Device>> ListDevicesAsync(CancellationToken cancellationToken = default) =>
        this._smartThingsClient.ListDevicesAsync(cancellationToken);

    public Task<DeviceState> GetDeviceStateAsync(string deviceId, CancellationToken cancellationToken = default) =>
        this._smartThingsClient.GetDeviceStatusAsync(deviceId, cancellationToken);

    public IReadOnlyList<ConsoleBinding> GetBindings(Device device, DeviceState? state = null) =>
        this._capabilityActionMapper.MapBindings(device, state);

    public async Task<CommandExecutionResult> SendCommandAsync(string deviceId, ConsoleBinding binding, DeviceState? currentState = null, CancellationToken cancellationToken = default)
    {
        if (binding.Kind is ConsoleBindingKind.Scene or ConsoleBindingKind.System)
        {
            return CommandExecutionResult.Fail("Binding is not a device command.", FailureKind.Validation);
        }

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return CommandExecutionResult.Fail("Device ID is required.", FailureKind.Validation);
        }

        try
        {
            var request = CreateCommand(binding, currentState);
            if (request is null)
            {
                return CommandExecutionResult.Fail("Device binding is read-only.", FailureKind.Unsupported);
            }

            await this._smartThingsClient.SendDeviceCommandAsync(deviceId, request, cancellationToken).ConfigureAwait(false);
            return CommandExecutionResult.Ok("Device command sent.");
        }
        catch (SmartThingsUnsupportedCapabilityException)
        {
            return CommandExecutionResult.Fail("Capability is not supported by this device.", FailureKind.Unsupported);
        }
        catch (SmartThingsAuthException)
        {
            return CommandExecutionResult.Fail("SmartThings authentication failed.", FailureKind.Auth);
        }
        catch (SmartThingsRateLimitException)
        {
            return CommandExecutionResult.Fail("SmartThings rate limit exceeded.", FailureKind.RateLimited);
        }
        catch (SmartThingsTransientException)
        {
            return CommandExecutionResult.Fail("SmartThings is temporarily unavailable.", FailureKind.Transient);
        }
        catch (SmartThingsException exception)
        {
            return CommandExecutionResult.Fail(exception.Message, FailureKind.Unknown);
        }
    }

    public DeviceCommandRequest? CreateCommand(ConsoleBinding binding, DeviceState? currentState = null)
    {
        if (binding.IsReadOnly)
        {
            return null;
        }

        var componentId = string.IsNullOrWhiteSpace(binding.ComponentId) ? "main" : binding.ComponentId;
        var capability = binding.Capability ?? string.Empty;

        return binding.ControlKind switch
        {
            DeviceControlKind.TogglePower => new DeviceCommandRequest(
                componentId,
                capability,
                ResolveTogglePowerCommand(currentState)),
            DeviceControlKind.TogglePlayback => new DeviceCommandRequest(
                componentId,
                capability,
                ResolveTogglePlaybackCommand(currentState)),
            DeviceControlKind.SetLevel => new DeviceCommandRequest(
                componentId,
                capability,
                binding.Command ?? "setLevel",
                new object?[] { 5 }),
            DeviceControlKind.ToggleMute => new DeviceCommandRequest(
                componentId,
                capability,
                ResolveToggleMuteCommand(currentState)),
            _ when !string.IsNullOrWhiteSpace(binding.Command) => new DeviceCommandRequest(
                componentId,
                capability,
                binding.Command),
            _ => null,
        };
    }

    private static string ResolveTogglePowerCommand(DeviceState? currentState)
    {
        if (currentState is not null &&
            currentState.TryGetString("switch", "switch", out var value) &&
            string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
        {
            return "off";
        }

        return "on";
    }

    private static string ResolveTogglePlaybackCommand(DeviceState? currentState)
    {
        if (currentState is null)
        {
            return "play";
        }

        foreach (var value in currentState.Values)
        {
            if (!string.Equals(value.CapabilityId, "mediaPlayback", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (value.AttributeName.Contains("play", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(value.Value, "playing", StringComparison.OrdinalIgnoreCase))
            {
                return "pause";
            }
        }

        return "play";
    }

    private static string ResolveToggleMuteCommand(DeviceState? currentState)
    {
        if (currentState is null)
        {
            return "mute";
        }

        foreach (var value in currentState.Values)
        {
            if (!string.Equals(value.CapabilityId, "audioMute", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (value.AttributeName.Contains("mute", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(value.Value, "muted", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(value.Value, "on", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(value.Value, "true", StringComparison.OrdinalIgnoreCase)))
            {
                return "unmute";
            }
        }

        return "mute";
    }
}
