using System.Text.Json;

namespace SmartThingsMxConsole.Core.Models;

public enum ConsoleBindingKind
{
    Scene,
    DeviceCommand,
    DeviceAdjustment,
    DeviceStatus,
    System,
}

public enum DeviceControlKind
{
    None,
    TogglePower,
    TurnOn,
    TurnOff,
    SetLevel,
    TogglePlayback,
    Play,
    Pause,
    Stop,
    NextTrack,
    PreviousTrack,
    VolumeUp,
    VolumeDown,
    ToggleMute,
    StatusOnly,
}

public enum DeviceCategory
{
    General,
    Lighting,
    Media,
    Laundry,
}

public sealed record ConsoleBinding(
    ConsoleBindingKind Kind,
    string EntityId,
    string DisplayName,
    string? ComponentId = "main",
    string? Capability = null,
    string? Command = null,
    string? LabelOverride = null,
    bool PinToFavorites = false,
    DeviceControlKind ControlKind = DeviceControlKind.None,
    DeviceCategory Category = DeviceCategory.General,
    bool IsReadOnly = false)
{
    public string EffectiveLabel => string.IsNullOrWhiteSpace(this.LabelOverride) ? this.DisplayName : this.LabelOverride;
}

public static class ConsoleBindingSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public static string Serialize(ConsoleBinding binding) => JsonSerializer.Serialize(binding, JsonOptions);

    public static ConsoleBinding Deserialize(string value) =>
        JsonSerializer.Deserialize<ConsoleBinding>(value, JsonOptions)
        ?? throw new InvalidOperationException("Failed to deserialize console binding.");

    public static bool TryDeserialize(string? value, out ConsoleBinding? binding)
    {
        binding = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            binding = Deserialize(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
