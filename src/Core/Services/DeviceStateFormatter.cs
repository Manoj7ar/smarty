using System.Globalization;
using SmartThingsMxConsole.Core.Abstractions;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Core.Services;

public sealed class DeviceStateFormatter : IDeviceStateFormatter
{
    public string FormatDeviceLabel(Device device, DeviceState? state, ConsoleBinding? binding = null)
    {
        var label = binding?.EffectiveLabel ?? device.DisplayName;

        if (state is null)
        {
            return $"{label} · Unavailable";
        }

        var status = TryFormatLaundry(state)
            ?? TryFormatMedia(state)
            ?? TryFormatLevel(state)
            ?? TryFormatSwitch(state)
            ?? TryFormatGeneric(state)
            ?? "Unavailable";

        if (state.IsStale)
        {
            status = $"{status} (stale)";
        }

        return $"{label} · {status}";
    }

    public string FormatSceneLabel(Scene scene, bool isReady = true) =>
        isReady ? $"{scene.DisplayName} · Ready" : $"{scene.DisplayName} · Unavailable";

    public string FormatPluginHealth(PluginHealthStatus status) => status.Message;

    private static string? TryFormatLaundry(DeviceState state)
    {
        if (TryGetRemainingTime(state, out var remaining))
        {
            return $"{remaining} left";
        }

        foreach (var capability in state.Values.Select(value => value.CapabilityId).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!capability.Contains("washer", StringComparison.OrdinalIgnoreCase) &&
                !capability.Contains("dryer", StringComparison.OrdinalIgnoreCase) &&
                !capability.Contains("operatingState", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var operatingState = state.GetValuesForCapability(capability)
                .FirstOrDefault(value =>
                    value.AttributeName.Contains("state", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(value.Value));

            if (operatingState is not null)
            {
                return Humanize(operatingState.Value!);
            }
        }

        return null;
    }

    private static string? TryFormatMedia(DeviceState state)
    {
        if (state.TryGetString("switch", "switch", out var switchValue) &&
            string.Equals(switchValue, "off", StringComparison.OrdinalIgnoreCase))
        {
            return "Off";
        }

        foreach (var value in state.Values)
        {
            if (!value.CapabilityId.Contains("media", StringComparison.OrdinalIgnoreCase) &&
                !value.CapabilityId.Contains("audio", StringComparison.OrdinalIgnoreCase) &&
                !value.CapabilityId.Contains("tv", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (value.AttributeName.Contains("play", StringComparison.OrdinalIgnoreCase) ||
                value.AttributeName.Contains("mute", StringComparison.OrdinalIgnoreCase))
            {
                return Humanize(value.Value ?? "On");
            }
        }

        return null;
    }

    private static string? TryFormatLevel(DeviceState state)
    {
        if (!state.TryGetDouble("switchLevel", "level", out var level))
        {
            return null;
        }

        return $"{Math.Round(level, MidpointRounding.AwayFromZero):0}%";
    }

    private static string? TryFormatSwitch(DeviceState state)
    {
        if (!state.TryGetString("switch", "switch", out var switchValue))
        {
            return null;
        }

        return Humanize(switchValue!);
    }

    private static string? TryFormatGeneric(DeviceState state)
    {
        var generic = state.Values.FirstOrDefault(value =>
            !string.IsNullOrWhiteSpace(value.Value) &&
            (value.AttributeName.Contains("status", StringComparison.OrdinalIgnoreCase) ||
             value.AttributeName.Contains("state", StringComparison.OrdinalIgnoreCase)));

        return generic?.Value is null ? null : Humanize(generic.Value);
    }

    private static bool TryGetRemainingTime(DeviceState state, out string value)
    {
        value = string.Empty;
        var candidate = state.Values.FirstOrDefault(item =>
            item.AttributeName.Contains("remaining", StringComparison.OrdinalIgnoreCase) ||
            item.AttributeName.Contains("completion", StringComparison.OrdinalIgnoreCase));

        if (candidate?.Value is null)
        {
            return false;
        }

        if (DateTimeOffset.TryParse(candidate.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var completionTime))
        {
            var remaining = completionTime - state.LastUpdatedUtc;
            if (remaining > TimeSpan.Zero)
            {
                value = $"{Math.Ceiling(remaining.TotalMinutes):0}m";
                return true;
            }
        }

        if (TimeSpan.TryParse(candidate.Value, CultureInfo.InvariantCulture, out var duration) && duration > TimeSpan.Zero)
        {
            value = $"{Math.Ceiling(duration.TotalMinutes):0}m";
            return true;
        }

        if (double.TryParse(candidate.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var minutes) && minutes > 0)
        {
            value = $"{Math.Ceiling(minutes):0}m";
            return true;
        }

        value = candidate.Value;
        return true;
    }

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unavailable";
        }

        value = value.Replace('_', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
    }
}
