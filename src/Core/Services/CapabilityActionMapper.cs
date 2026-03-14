using SmartThingsMxConsole.Core.Abstractions;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Core.Services;

public sealed class CapabilityActionMapper : ICapabilityActionMapper
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public DeviceCategory Classify(Device device)
    {
        if (device.Capabilities.Any(capability => IsLaundryCapability(capability.CapabilityId)))
        {
            return DeviceCategory.Laundry;
        }

        if (device.Capabilities.Any(capability => IsMediaCapability(capability.CapabilityId)))
        {
            return DeviceCategory.Media;
        }

        if (device.HasCapability("switchLevel") || device.HasCapability("switch"))
        {
            return DeviceCategory.Lighting;
        }

        return DeviceCategory.General;
    }

    public IReadOnlyList<ConsoleBinding> MapBindings(Device device, DeviceState? state = null)
    {
        var category = this.Classify(device);
        var bindings = new List<ConsoleBinding>();

        if (device.HasCapability("switch"))
        {
            bindings.Add(new ConsoleBinding(
                ConsoleBindingKind.DeviceCommand,
                device.Id,
                device.DisplayName,
                Capability: "switch",
                Command: "toggle",
                ControlKind: DeviceControlKind.TogglePower,
                Category: category));
        }

        if (device.HasCapability("switchLevel"))
        {
            bindings.Add(new ConsoleBinding(
                ConsoleBindingKind.DeviceAdjustment,
                device.Id,
                $"{device.DisplayName} Brightness",
                Capability: "switchLevel",
                Command: "setLevel",
                ControlKind: DeviceControlKind.SetLevel,
                Category: category));
        }

        AddMediaBindings(device, bindings, category);

        if (bindings.Count == 0 || category == DeviceCategory.Laundry)
        {
            bindings.Add(new ConsoleBinding(
                ConsoleBindingKind.DeviceStatus,
                device.Id,
                device.DisplayName,
                Capability: SelectStatusCapability(device),
                Command: null,
                ControlKind: DeviceControlKind.StatusOnly,
                Category: category,
                IsReadOnly: true));
        }

        return bindings;
    }

    private static void AddMediaBindings(Device device, ICollection<ConsoleBinding> bindings, DeviceCategory category)
    {
        var mediaPlayback = device.GetCapability("mediaPlayback");
        if (mediaPlayback is not null)
        {
            if (mediaPlayback.SupportsCommand("play") && mediaPlayback.SupportsCommand("pause"))
            {
                bindings.Add(new ConsoleBinding(
                    ConsoleBindingKind.DeviceCommand,
                    device.Id,
                    $"{device.DisplayName} Play/Pause",
                    Capability: mediaPlayback.CapabilityId,
                    Command: "togglePlayback",
                    ControlKind: DeviceControlKind.TogglePlayback,
                    Category: category));
            }

            AddCapabilityCommand(mediaPlayback, "play", DeviceControlKind.Play, device, bindings, category);
            AddCapabilityCommand(mediaPlayback, "pause", DeviceControlKind.Pause, device, bindings, category);
            AddCapabilityCommand(mediaPlayback, "stop", DeviceControlKind.Stop, device, bindings, category);
        }

        var trackControl = device.GetCapability("mediaTrackControl");
        if (trackControl is not null)
        {
            AddCapabilityCommand(trackControl, "nextTrack", DeviceControlKind.NextTrack, device, bindings, category);
            AddCapabilityCommand(trackControl, "previousTrack", DeviceControlKind.PreviousTrack, device, bindings, category);
        }

        var audioVolume = device.GetCapability("audioVolume");
        if (audioVolume is not null)
        {
            AddCapabilityCommand(audioVolume, "volumeUp", DeviceControlKind.VolumeUp, device, bindings, category);
            AddCapabilityCommand(audioVolume, "volumeDown", DeviceControlKind.VolumeDown, device, bindings, category);
        }

        var audioMute = device.GetCapability("audioMute");
        if (audioMute is not null)
        {
            AddCapabilityCommand(audioMute, "mute", DeviceControlKind.ToggleMute, device, bindings, category, "Mute Toggle");
        }
    }

    private static void AddCapabilityCommand(
        CapabilitySummary capability,
        string command,
        DeviceControlKind controlKind,
        Device device,
        ICollection<ConsoleBinding> bindings,
        DeviceCategory category,
        string? suffix = null)
    {
        if (!capability.SupportsCommand(command))
        {
            return;
        }

        bindings.Add(new ConsoleBinding(
            ConsoleBindingKind.DeviceCommand,
            device.Id,
            suffix is null ? $"{device.DisplayName} {Humanize(command)}" : $"{device.DisplayName} {suffix}",
            Capability: capability.CapabilityId,
            Command: command,
            ControlKind: controlKind,
            Category: category));
    }

    private static string SelectStatusCapability(Device device)
    {
        var laundryCapability = device.Capabilities.FirstOrDefault(capability => IsLaundryCapability(capability.CapabilityId));
        if (laundryCapability is not null)
        {
            return laundryCapability.CapabilityId;
        }

        var switchCapability = device.GetCapability("switch");
        return switchCapability?.CapabilityId
            ?? device.Capabilities.FirstOrDefault()?.CapabilityId
            ?? "status";
    }

    private static bool IsLaundryCapability(string capabilityId) =>
        capabilityId.Contains("washer", StringComparison.OrdinalIgnoreCase) ||
        capabilityId.Contains("dryer", StringComparison.OrdinalIgnoreCase) ||
        capabilityId.Contains("operatingState", StringComparison.OrdinalIgnoreCase);

    private static bool IsMediaCapability(string capabilityId) =>
        capabilityId.Contains("media", StringComparison.OrdinalIgnoreCase) ||
        capabilityId.Contains("audio", StringComparison.OrdinalIgnoreCase) ||
        capabilityId.Contains("tv", StringComparison.OrdinalIgnoreCase);

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var characters = new List<char>(value.Length + 4);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (index > 0 && char.IsUpper(character) && !char.IsUpper(value[index - 1]))
            {
                characters.Add(' ');
            }

            characters.Add(index == 0 ? char.ToUpperInvariant(character) : character);
        }

        return new string(characters.ToArray());
    }
}
