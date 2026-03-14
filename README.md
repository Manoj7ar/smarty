# SmartThings MX Console

SmartThings MX Console is a production-leaning MVP plugin for Logitech MX Creative Console built with the Logi Actions SDK on .NET 8. It connects to SmartThings Cloud with a local Personal Access Token, exposes SmartThings scenes and device controls as MX Console actions, and keeps selected device labels refreshed with polling-based live status.

## What is included

- SmartThings PAT-based local authentication behind an `IAuthProvider` abstraction.
- SmartThings HTTP client for scenes, devices, status, and commands.
- Capability-driven device mapping for:
  - `switch`
  - `switchLevel`
  - `mediaPlayback`
  - `mediaTrackControl`
  - `audioVolume`
  - `audioMute`
  - washer/dryer operating-state capabilities
- Logitech plugin commands for:
  - scenes
  - devices
  - dimmer level adjustment
  - plugin status
  - metadata refresh
  - cache clear
  - configuration via Action Editor
- Grouped action sections for `Scenes`, `Devices`, `Favorites`, `Laundry`, `TV / Media`, and `System`.
- Polling-based live device status with stale-state handling and last-known-state retention.
- Local settings, metadata cache, and local PAT storage.
- Automated tests for mapping, formatting, scene execution, polling, SmartThings parsing, command payloads, and storage.

## Repository layout

- `src/Core`: domain models, abstractions, use cases, capability mapping, formatting, polling.
- `src/SmartThingsClient`: SmartThings HTTP client, storage, auth providers, runtime factory.
- `src/Plugin`: Logitech plugin entrypoint, actions, bindings, configuration command, package metadata.
- `tests/SmartThingsMxConsole.Tests`: xUnit coverage for core and SmartThings client behavior.
- `config`: example local settings.
- `scripts`: local development helpers.

## Current Logitech UX

This MVP uses grouped dynamic actions plus an Action Editor configuration command as the primary browse-and-assign surface. The grouping maps to the requested sections:

- `Scenes`
- `Devices`
- `Favorites`
- `Laundry`
- `TV / Media`
- `System`

That keeps the SmartThings integration stable while isolating Logitech-specific UX behind the plugin layer. If you want a deeper profile-action tree later, the right extension point is the plugin adapter rather than the SmartThings client or core services.

## Prerequisites

- Windows with Logitech plugin service installed.
- .NET 8 SDK.
- `PluginApi.dll` available at `C:\Program Files\Logi\LogiPluginService\PluginApi.dll`.
- A SmartThings Personal Access Token with access to scenes and devices.

## Local storage

The plugin stores local state in `%LOCALAPPDATA%\SmartThingsMxConsole`:

- `settings.json`: refresh interval, favorites, polling settings.
- `cache.json`: cached scenes, devices, and last-known device snapshots.
- `secrets.json`: local PAT payload keyed by `smartthings-pat`.

The PAT is intentionally not stored in `settings.json`.

## Setup

### Option 1: helper script

```powershell
.\scripts\set-smartthings-config.ps1 -PersonalAccessToken "<smartthings-pat>"
```

Optional parameters:

```powershell
.\scripts\set-smartthings-config.ps1 -PersonalAccessToken "<smartthings-pat>" -RefreshInterval "00:00:20"
.\scripts\set-smartthings-config.ps1 -PersonalAccessToken "<smartthings-pat>" -PollAllDevices
```

### Option 2: plugin configuration action

Build and load the plugin, then run the `Configure SmartThings` action from the `System` group. That Action Editor flow lets you:

- paste or replace the PAT
- set refresh interval
- add a favorite scene or device
- clear cached metadata

## Build

```powershell
dotnet build SmartThingsMxConsole.sln
```

To build just the Logitech plugin:

```powershell
dotnet build src\Plugin\Plugin.csproj
```

The plugin project writes a `.link` file into the local Logitech plugin directory and attempts a reload after build.

## Run locally

1. Install the PAT with the helper script or the plugin configuration action.
2. Build the plugin project.
3. Open Logitech MX Creative Console / Logi Actions.
4. Assign actions from the `SmartThings MX Console` plugin:
   - `SmartThings Scenes`
   - `SmartThings Devices`
   - `SmartThings Level`
   - `SmartThings Status`
   - `Refresh SmartThings`
   - `Clear SmartThings Cache`
- `Configure SmartThings` if enabled in your build

Scene actions execute the selected scene directly. Device actions are generated dynamically from SmartThings capabilities and grouped by section. Status labels update from the polling cache for bound or favorited devices.

## Test

```powershell
dotnet test tests\SmartThingsMxConsole.Tests\SmartThingsMxConsole.Tests.csproj
```

## Example config

`config/settings.example.json` shows the non-secret settings shape:

```json
{
  "providerName": "PAT",
  "refreshInterval": "00:00:15",
  "pollAssignedDevicesOnly": true,
  "favorites": []
}
```

## Architecture notes

- `ISmartThingsClient` isolates all SmartThings REST calls.
- `IAuthProvider` keeps PAT and future OAuth flows behind one application-facing abstraction.
- `CapabilityActionMapper` turns device capabilities into console bindings without hardcoded device models.
- `DeviceStateFormatter` compresses SmartThings status into console-safe labels like `Washer - 18m left`.
- `PollingCoordinator` only watches assigned or favorited devices and marks states stale after repeated refresh failures.
- `MetadataService` separates device/scene metadata caching from live state polling.

## Troubleshooting

### Plugin loads but shows setup state

- Verify `%LOCALAPPDATA%\SmartThingsMxConsole\secrets.json` exists.
- Re-run `.\scripts\set-smartthings-config.ps1`.
- Trigger `Refresh SmartThings` or `SmartThings Status`.

### Invalid auth

- Generate a fresh SmartThings PAT.
- Re-save it with the helper script or `Configure SmartThings`.
- Refresh metadata again.

### No scenes or devices appear

- Confirm the SmartThings account actually has scenes/devices visible to the PAT.
- Use `Clear SmartThings Cache`, then `Refresh SmartThings`.
- Check whether the PAT has the required SmartThings API access.

### Device labels stop updating

- The polling service only watches bound or favorited devices by design.
- Assign the action to a console key or add it as a favorite, then refresh.
- If SmartThings is degraded, stale labels are retained instead of being cleared.

### Logitech build issues

- Confirm `PluginApi.dll` exists in `C:\Program Files\Logi\LogiPluginService\`.
- Rebuild with `dotnet build src\Plugin\Plugin.csproj`.
- If Logitech does not reload automatically, restart the Logitech plugin service or the host app.

## Production hardening next steps

- Replace PAT-only development auth with a real OAuth 2.0 flow and token refresh.
- Preserve SmartThings component IDs in capability metadata for multi-component devices that expose the same capability in multiple places.
- Add richer Logitech profile browsing if the SDK tree APIs become a better fit than grouped dynamic parameters.
- Add structured logging and optional diagnostics export.
- Add packaging polish for marketplace submission, branding assets, and versioned release flow.
- Consider webhook or event-subscription support to reduce polling for high-frequency devices.

## Assumptions

- Windows-first development is acceptable for v1.
- PAT-based local development is the right first release shape.
- Polling only assigned or favorited devices is preferred over polling the entire SmartThings account.
- Favorites are explicit plugin-managed pins, not an automatic mirror of SmartThings favorites.
- Room/category mapping stays lightweight unless SmartThings metadata exposes it directly.
