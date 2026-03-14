using System.Collections.Concurrent;
using System.Runtime.Versioning;
using SmartThingsClient;
using SmartThingsClient.Configuration;
using SmartThingsMxConsole.Core.Abstractions;
using SmartThingsMxConsole.Core.Exceptions;
using SmartThingsMxConsole.Core.Models;
using SmartThingsMxConsole.Core.Services;

namespace SmartThingsMxConsole.Plugin;

internal enum RegisteredActionKind
{
    Command,
    Adjustment,
}

internal sealed record RegisteredAction(
    string ActionName,
    string ActionParameter,
    ConsoleBinding Binding,
    RegisteredActionKind Kind);

internal sealed record ListOption(string Name, string DisplayName, string Description);

internal sealed class SmartThingsPluginContext : IAsyncDisposable
{
    private readonly SmartThingsMxConsolePlugin _plugin;
    private readonly SemaphoreSlim _syncGate = new(1, 1);
    private readonly SmartThingsRuntime _runtime;
    private readonly SceneService _sceneService;
    private readonly DeviceService _deviceService;
    private readonly MetadataService _metadataService;
    private readonly IDeviceStateFormatter _deviceStateFormatter;
    private readonly IPollingCoordinator _pollingCoordinator;
    private readonly ConcurrentDictionary<string, RegisteredAction> _registeredActions = new(StringComparer.OrdinalIgnoreCase);
    private MetadataSnapshot _metadataSnapshot = new();
    private LocalSettings _settings = LocalSettings.Default;
    private AuthConfig _authConfig = AuthConfig.SetupRequired();
    private PluginHealthStatus _pluginHealth = new(PluginHealthState.SetupRequired, "Add a SmartThings PAT to start.");
    private IReadOnlyDictionary<string, IReadOnlyList<ConsoleBinding>> _bindingsByDevice =
        new Dictionary<string, IReadOnlyList<ConsoleBinding>>(StringComparer.OrdinalIgnoreCase);

    public SmartThingsPluginContext(SmartThingsMxConsolePlugin plugin)
    {
        PluginDiagnostics.Write("SmartThingsPluginContext constructor start");
        _plugin = plugin;
        _runtime = SmartThingsRuntimeFactory.CreateDefault();
        _sceneService = new SceneService(_runtime.Client);
        _deviceService = new DeviceService(_runtime.Client, new CapabilityActionMapper());
        _metadataService = new MetadataService(_runtime.Client, _runtime.MetadataCache);
        _deviceStateFormatter = new DeviceStateFormatter();
        _pollingCoordinator = new PollingCoordinator(_runtime.Client, new SystemClock());
        _pollingCoordinator.DeviceStateChanged += HandleDeviceStateChanged;
        PluginDiagnostics.Write("SmartThingsPluginContext constructor complete");
    }

    public SmartThingsDataPaths DataPaths => _runtime.DataPaths;

    public PluginHealthStatus PluginHealth => _pluginHealth;

    public LocalSettings Settings => _settings;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        PluginDiagnostics.Write("Context LoadAsync start");
        _settings = await _runtime.SettingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        _pollingCoordinator.SetRefreshInterval(_settings.RefreshInterval);
        _authConfig = await _runtime.AuthProvider.GetAuthConfigAsync(cancellationToken).ConfigureAwait(false);

        var cached = await _metadataService.LoadCachedAsync(cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            _metadataSnapshot = cached;
        }

        RebuildBindings();
        await _pollingCoordinator.StartAsync(cancellationToken).ConfigureAwait(false);
        await RefreshMetadataAsync(false, cancellationToken).ConfigureAwait(false);
        await UpdateWatchedBindingsAsync(cancellationToken).ConfigureAwait(false);
        PublishDisplayState();
        PluginDiagnostics.Write("Context LoadAsync complete");
    }

    public async Task RefreshMetadataAsync(bool forceRefresh, CancellationToken cancellationToken = default)
    {
        await _syncGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _authConfig = await _runtime.AuthProvider.GetAuthConfigAsync(cancellationToken).ConfigureAwait(false);
            if (!_authConfig.IsConfigured)
            {
                _pluginHealth = new PluginHealthStatus(
                    PluginHealthState.SetupRequired,
                    "SmartThings setup required. Use Configure SmartThings or scripts/set-smartthings-config.ps1.");
                RebuildBindings();
                PublishMetadataState();
                return;
            }

            try
            {
                if (forceRefresh || _metadataSnapshot.Devices.Count == 0 || _metadataSnapshot.Scenes.Count == 0)
                {
                    _metadataSnapshot = await _metadataService.RefreshAsync(cancellationToken).ConfigureAwait(false);
                }

                _pluginHealth = new PluginHealthStatus(PluginHealthState.Connected, "Connected to SmartThings.");
            }
            catch (SmartThingsAuthException exception)
            {
                _pluginHealth = new PluginHealthStatus(PluginHealthState.InvalidAuth, exception.Message);
            }
            catch (SmartThingsTransientException exception)
            {
                _pluginHealth = new PluginHealthStatus(PluginHealthState.Degraded, exception.Message);
            }
            catch (SmartThingsException exception)
            {
                _pluginHealth = new PluginHealthStatus(PluginHealthState.Error, exception.Message);
            }

            RebuildBindings();
            PublishMetadataState();
        }
        finally
        {
            _syncGate.Release();
        }
    }

    public async Task ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        await _metadataService.ClearAsync(cancellationToken).ConfigureAwait(false);
        _metadataSnapshot = new MetadataSnapshot();
        RebuildBindings();
        PublishMetadataState();
    }

    public async Task SaveConfigurationAsync(
        string? personalAccessToken,
        int refreshIntervalSeconds,
        string? favoriteType,
        string? favoriteTargetId,
        bool clearCache,
        CancellationToken cancellationToken = default)
    {
        var updatedSettings = _settings with
        {
            RefreshInterval = TimeSpan.FromSeconds(Math.Clamp(refreshIntervalSeconds, 5, 300)),
            Favorites = UpdateFavorites(favoriteType, favoriteTargetId)
        };

        if (!string.IsNullOrWhiteSpace(personalAccessToken))
        {
            await _runtime.SecretStore.SaveSecretAsync(
                SmartThingsDataPaths.DefaultPersonalAccessTokenSecretName,
                personalAccessToken,
                cancellationToken).ConfigureAwait(false);
        }

        await _runtime.SettingsStore.SaveAsync(updatedSettings, cancellationToken).ConfigureAwait(false);
        _settings = updatedSettings;
        _pollingCoordinator.SetRefreshInterval(_settings.RefreshInterval);

        if (clearCache)
        {
            await _metadataService.ClearAsync(cancellationToken).ConfigureAwait(false);
        }

        await RefreshMetadataAsync(true, cancellationToken).ConfigureAwait(false);
        await UpdateWatchedBindingsAsync(cancellationToken).ConfigureAwait(false);
        await _pollingCoordinator.RefreshNowAsync(cancellationToken).ConfigureAwait(false);
    }

    public IReadOnlyList<ConsoleBinding> GetSceneBindings()
    {
        if (!_authConfig.IsConfigured)
        {
            return [CreateSystemBinding("setup-scenes", "Setup SmartThings", "Configure SmartThings to load scenes.")];
        }

        if (_metadataSnapshot.Scenes.Count == 0)
        {
            return [CreateSystemBinding("empty-scenes", "No scenes found", "No SmartThings scenes are available.")];
        }

        return _metadataSnapshot.Scenes
            .Select(scene => new ConsoleBinding(ConsoleBindingKind.Scene, scene.Id, scene.DisplayName))
            .ToArray();
    }

    public IReadOnlyList<ConsoleBinding> GetDeviceBindings()
    {
        if (!_authConfig.IsConfigured)
        {
            return [CreateSystemBinding("setup-devices", "Setup SmartThings", "Configure SmartThings to load devices.")];
        }

        var bindings = _bindingsByDevice.Values.SelectMany(static value => value).ToList();
        if (bindings.Count == 0)
        {
            return [CreateSystemBinding("empty-devices", "No devices found", "No SmartThings devices are available.")];
        }

        return _settings.Favorites
            .Select(favorite => favorite.Binding)
            .Concat(bindings)
            .Distinct()
            .ToArray();
    }

    public IReadOnlyList<ConsoleBinding> GetLevelBindings() =>
        GetDeviceBindings()
            .Where(binding => binding.ControlKind == DeviceControlKind.SetLevel)
            .ToArray();

    public IReadOnlyList<ListOption> GetFavoriteTypeOptions() =>
        [
            new("none", "No favorite update", "Keep favorites unchanged."),
            new("scene", "Add scene favorite", "Pin a SmartThings scene."),
            new("device", "Add device favorite", "Pin a SmartThings device.")
        ];

    public IReadOnlyList<ListOption> GetFavoriteTargetOptions(string? favoriteType)
    {
        return favoriteType?.ToLowerInvariant() switch
        {
            "scene" => _metadataSnapshot.Scenes
                .Select(scene => new ListOption(scene.Id, scene.DisplayName, "Scene favorite"))
                .ToArray(),
            "device" => _metadataSnapshot.Devices
                .Select(device => new ListOption(device.Id, device.DisplayName, "Device favorite"))
                .ToArray(),
            _ => Array.Empty<ListOption>()
        };
    }

    public string GetGroupName(ConsoleBinding binding, string defaultGroup)
    {
        if (IsFavorite(binding))
        {
            return "Favorites";
        }

        return binding.Category switch
        {
            DeviceCategory.Laundry => "Laundry",
            DeviceCategory.Media => "TV / Media",
            _ => defaultGroup
        };
    }

    public string GetBindingLabel(ConsoleBinding binding)
    {
        if (binding.Kind == ConsoleBindingKind.System)
        {
            return _pluginHealth.Message;
        }

        if (binding.Kind == ConsoleBindingKind.Scene)
        {
            var scene = _metadataSnapshot.Scenes.FirstOrDefault(item => string.Equals(item.Id, binding.EntityId, StringComparison.OrdinalIgnoreCase))
                ?? new Scene(binding.EntityId, binding.EffectiveLabel);
            return _deviceStateFormatter.FormatSceneLabel(scene, _authConfig.IsConfigured);
        }

        var device = _metadataSnapshot.Devices.FirstOrDefault(item => string.Equals(item.Id, binding.EntityId, StringComparison.OrdinalIgnoreCase))
            ?? new Device(binding.EntityId, binding.EffectiveLabel, binding.EffectiveLabel);
        return _deviceStateFormatter.FormatDeviceLabel(device, GetDeviceState(device.Id), binding);
    }

    public double GetAdjustmentLevel(ConsoleBinding binding)
    {
        var state = GetDeviceState(binding.EntityId);
        return state is not null && state.TryGetDouble("switchLevel", "level", out var level)
            ? Math.Clamp(level, 0, 100)
            : 0;
    }

    public void TrackBinding(string actionName, string actionParameter, ConsoleBinding binding, RegisteredActionKind kind)
    {
        var key = $"{actionName}|{actionParameter}";
        _registeredActions[key] = new RegisteredAction(actionName, actionParameter, binding, kind);
        _ = UpdateWatchedBindingsAsync(CancellationToken.None);
    }

    public async Task<CommandExecutionResult> ExecuteBindingAsync(ConsoleBinding binding, CancellationToken cancellationToken = default)
    {
        switch (binding.Kind)
        {
            case ConsoleBindingKind.System:
                return CommandExecutionResult.Ok(_pluginHealth.Message);

            case ConsoleBindingKind.Scene:
                var sceneResult = await _sceneService.ExecuteSceneAsync(binding.EntityId, cancellationToken).ConfigureAwait(false);
                UpdateCommandFeedback(sceneResult);
                return sceneResult;

            case ConsoleBindingKind.DeviceStatus:
                await RefreshSingleDeviceAsync(binding.EntityId, cancellationToken).ConfigureAwait(false);
                var statusResult = CommandExecutionResult.Ok("Device status refreshed.");
                UpdateCommandFeedback(statusResult);
                return statusResult;

            case ConsoleBindingKind.DeviceAdjustment:
            case ConsoleBindingKind.DeviceCommand:
                var result = await _deviceService.SendCommandAsync(binding.EntityId, binding, GetDeviceState(binding.EntityId), cancellationToken).ConfigureAwait(false);
                if (result.Success)
                {
                    await RefreshSingleDeviceAsync(binding.EntityId, cancellationToken).ConfigureAwait(false);
                }

                UpdateCommandFeedback(result);
                return result;

            default:
                return CommandExecutionResult.Fail("Unsupported binding.", FailureKind.Unsupported);
        }
    }

    public async Task<CommandExecutionResult> ApplyLevelAsync(ConsoleBinding binding, int diff, CancellationToken cancellationToken = default)
    {
        var currentLevel = GetAdjustmentLevel(binding);
        var newLevel = Math.Clamp(currentLevel + (diff * 5), 0, 100);
        var request = new DeviceCommandRequest(
            string.IsNullOrWhiteSpace(binding.ComponentId) ? "main" : binding.ComponentId!,
            binding.Capability ?? "switchLevel",
            binding.Command ?? "setLevel",
            [Math.Round(newLevel, MidpointRounding.AwayFromZero)]);

        try
        {
            await _runtime.Client.SendDeviceCommandAsync(binding.EntityId, request, cancellationToken).ConfigureAwait(false);
            await RefreshSingleDeviceAsync(binding.EntityId, cancellationToken).ConfigureAwait(false);
            var result = CommandExecutionResult.Ok("Level updated.");
            UpdateCommandFeedback(result);
            return result;
        }
        catch (SmartThingsException exception)
        {
            var result = CommandExecutionResult.Fail(exception.Message, FailureKind.Unknown);
            UpdateCommandFeedback(result);
            return result;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _pollingCoordinator.DeviceStateChanged -= HandleDeviceStateChanged;
        await _pollingCoordinator.DisposeAsync().ConfigureAwait(false);
        _runtime.Dispose();
        _syncGate.Dispose();
    }

    private async Task RefreshSingleDeviceAsync(string deviceId, CancellationToken cancellationToken)
    {
        try
        {
            var state = await _runtime.Client.GetDeviceStatusAsync(deviceId, cancellationToken).ConfigureAwait(false);
            UpdateDeviceState(state);
            PublishDisplayState();
        }
        catch (SmartThingsException)
        {
        }
    }

    private void HandleDeviceStateChanged(object? sender, DeviceStateChangedEventArgs eventArgs)
    {
        UpdateDeviceState(eventArgs.State);
        PublishDisplayState();
    }

    private void UpdateDeviceState(DeviceState state)
    {
        var states = new Dictionary<string, DeviceState>(_metadataSnapshot.DeviceStates, StringComparer.OrdinalIgnoreCase)
        {
            [state.DeviceId] = state
        };

        _metadataSnapshot = _metadataSnapshot with { DeviceStates = states };
    }

    private void RebuildBindings()
    {
        var bindings = new Dictionary<string, IReadOnlyList<ConsoleBinding>>(StringComparer.OrdinalIgnoreCase);
        foreach (var device in _metadataSnapshot.Devices)
        {
            bindings[device.Id] = _deviceService.GetBindings(device, GetDeviceState(device.Id));
        }

        _bindingsByDevice = bindings;
    }

    private DeviceState? GetDeviceState(string deviceId)
    {
        if (_pollingCoordinator.TryGetLatestState(deviceId, out var latestState) && latestState is not null)
        {
            return latestState;
        }

        _metadataSnapshot.DeviceStates.TryGetValue(deviceId, out var cachedState);
        return cachedState;
    }

    private IReadOnlyList<FavoriteAssignment> UpdateFavorites(string? favoriteType, string? favoriteTargetId)
    {
        if (string.IsNullOrWhiteSpace(favoriteType) || string.Equals(favoriteType, "none", StringComparison.OrdinalIgnoreCase))
        {
            return _settings.Favorites;
        }

        ConsoleBinding? binding = favoriteType.ToLowerInvariant() switch
        {
            "scene" => _metadataSnapshot.Scenes
                .Where(scene => string.Equals(scene.Id, favoriteTargetId, StringComparison.OrdinalIgnoreCase))
                .Select(scene => new ConsoleBinding(ConsoleBindingKind.Scene, scene.Id, scene.DisplayName, PinToFavorites: true))
                .FirstOrDefault(),
            "device" => CreateFavoriteDeviceBinding(favoriteTargetId),
            _ => null
        };

        if (binding is null || _settings.Favorites.Any(existing => existing.Binding == binding))
        {
            return _settings.Favorites;
        }

        return _settings.Favorites
            .Append(new FavoriteAssignment(
                binding.EntityId,
                binding.Kind,
                binding.EffectiveLabel,
                binding.Category,
                binding,
                DateTimeOffset.UtcNow))
            .ToArray();
    }

    private bool IsFavorite(ConsoleBinding binding) =>
        binding.PinToFavorites ||
        _settings.Favorites.Any(favorite => favorite.Binding == binding);

    private ConsoleBinding CreateSystemBinding(string entityId, string displayName, string description) =>
        new(ConsoleBindingKind.System, entityId, displayName, Command: description, ControlKind: DeviceControlKind.StatusOnly, IsReadOnly: true);

    private async Task UpdateWatchedBindingsAsync(CancellationToken cancellationToken)
    {
        var watchedBindings = _registeredActions.Values
            .Select(registration => registration.Binding)
            .Concat(_settings.Favorites.Select(favorite => favorite.Binding))
            .Where(binding => binding.Kind is ConsoleBindingKind.DeviceCommand or ConsoleBindingKind.DeviceAdjustment or ConsoleBindingKind.DeviceStatus)
            .Distinct()
            .ToArray();

        await _pollingCoordinator.UpdateWatchedBindingsAsync(watchedBindings, cancellationToken).ConfigureAwait(false);
    }

    private void PublishMetadataState()
    {
        PublishPluginStatus();
        SmartThingsPluginEvents.RaiseMetadataChanged();
        SmartThingsPluginEvents.RaiseDisplayStateChanged();
    }

    private void PublishDisplayState() => SmartThingsPluginEvents.RaiseDisplayStateChanged();

    private void UpdateCommandFeedback(CommandExecutionResult result)
    {
        _pluginHealth = new PluginHealthStatus(
            result.FailureKind switch
            {
                FailureKind.None => PluginHealthState.Connected,
                FailureKind.Auth => PluginHealthState.InvalidAuth,
                FailureKind.RateLimited or FailureKind.Transient => PluginHealthState.Degraded,
                _ => PluginHealthState.Error,
            },
            result.Message);

        PublishPluginStatus();
        PublishDisplayState();
    }

    private void PublishPluginStatus()
    {
        var status = _pluginHealth.State switch
        {
            PluginHealthState.Connected => Loupedeck.PluginStatus.Normal,
            PluginHealthState.SetupRequired => Loupedeck.PluginStatus.Warning,
            PluginHealthState.Degraded => Loupedeck.PluginStatus.Warning,
            PluginHealthState.InvalidAuth => Loupedeck.PluginStatus.Error,
            _ => Loupedeck.PluginStatus.Error,
        };

        _plugin.UpdatePluginStatus(status, _pluginHealth.Message);
    }

    private ConsoleBinding? CreateFavoriteDeviceBinding(string? favoriteTargetId)
    {
        var selectedBinding = GetDeviceBindings()
            .Where(deviceBinding => deviceBinding.Kind != ConsoleBindingKind.System &&
                                    string.Equals(deviceBinding.EntityId, favoriteTargetId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(bindingItem => bindingItem.ControlKind == DeviceControlKind.StatusOnly ? 1 : 0)
            .FirstOrDefault();

        return selectedBinding is null
            ? null
            : selectedBinding with { PinToFavorites = true };
    }
}
