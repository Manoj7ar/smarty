using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SmartThingsClient.Configuration;
using SmartThingsClient.Dto;
using SmartThingsClient.Internal;
using SmartThingsMxConsole.Core.Abstractions;
using SmartThingsMxConsole.Core.Exceptions;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsClient;

public sealed class SmartThingsHttpClient(
    HttpClient httpClient,
    IBearerTokenProvider authProvider,
    SmartThingsClientOptions? options = null) : ISmartThingsClient
{
    private readonly SmartThingsClientOptions _options = options ?? SmartThingsClientOptions.Default;

    public async Task<IReadOnlyList<Scene>> ListScenesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "scenes", null, cancellationToken).ConfigureAwait(false);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<SmartThingsListResponse<SmartThingsSceneDto>>(stream, JsonOptions.Default, cancellationToken).ConfigureAwait(false);
        return payload?.Items?
            .Where(item => !string.IsNullOrWhiteSpace(item.SceneId))
            .Select(item => new Scene(
                item.SceneId!,
                string.IsNullOrWhiteSpace(item.SceneName) ? item.SceneId! : item.SceneName!,
                item.LocationId))
            .ToList() ?? [];
    }

    public async Task ExecuteSceneAsync(string sceneId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneId);
        using var _ = await SendAsync(HttpMethod.Post, $"scenes/{Uri.EscapeDataString(sceneId)}/execute", null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Device>> ListDevicesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "devices", null, cancellationToken).ConfigureAwait(false);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<SmartThingsListResponse<SmartThingsDeviceDto>>(stream, JsonOptions.Default, cancellationToken).ConfigureAwait(false);

        return payload?.Items?
            .Where(item => !string.IsNullOrWhiteSpace(item.DeviceId))
            .Select(item => new Device(
                item.DeviceId!,
                item.Name ?? item.Label ?? item.DeviceId!,
                item.Label ?? item.Name ?? item.DeviceId!,
                item.LocationId,
                item.RoomId,
                null,
                item.DeviceTypeName,
                item.Components?
                    .Where(component => !string.IsNullOrWhiteSpace(component.Id))
                    .SelectMany(component => component.Capabilities?
                        .Where(capability => !string.IsNullOrWhiteSpace(capability.Id))
                        .Select(capability => CreateCapabilitySummary(capability.Id!, capability.Version))
                        ?? [])
                    .ToList() ?? []))
            .ToList() ?? [];
    }

    public async Task<DeviceState> GetDeviceStatusAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        using var response = await SendAsync(HttpMethod.Get, $"devices/{Uri.EscapeDataString(deviceId)}/status", null, cancellationToken).ConfigureAwait(false);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return document.RootElement.ToDeviceState(deviceId);
    }

    public async Task SendDeviceCommandAsync(string deviceId, DeviceCommandRequest command, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentNullException.ThrowIfNull(command);

        var request = SmartThingsCommandRequest.FromCommands([command]);
        using var content = JsonContent.Create(request, options: JsonOptions.Default);
        using var _ = await SendAsync(
            HttpMethod.Post,
            $"devices/{Uri.EscapeDataString(deviceId)}/commands",
            content,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativePath,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        try
        {
            var accessToken = await authProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new SmartThingsAuthException("SmartThings authentication is not configured.");
            }

            var request = new HttpRequestMessage(method, new Uri(_options.BaseAddress, relativePath))
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.ParseAdd(_options.UserAgent);

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            response.Dispose();
            throw CreateException(response.StatusCode, responseContent);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SmartThingsTransientException("The SmartThings request timed out.");
        }
        catch (HttpRequestException exception)
        {
            throw new SmartThingsTransientException("The SmartThings request failed due to a network error.", exception);
        }
    }

    private static CapabilitySummary CreateCapabilitySummary(string capabilityId, int version)
    {
        var normalizedCapabilityId = capabilityId.ToLowerInvariant();
        var commands = normalizedCapabilityId switch
        {
            "switch" => new[] { "on", "off" },
            "switchlevel" => new[] { "setLevel" },
            "mediaplayback" => new[] { "play", "pause", "stop" },
            "mediatrackcontrol" => new[] { "nextTrack", "previousTrack" },
            "audiovolume" => new[] { "volumeUp", "volumeDown" },
            "audiomute" => new[] { "mute", "unmute" },
            _ => Array.Empty<string>()
        };

        var attributes = normalizedCapabilityId switch
        {
            "switch" => new[] { "switch" },
            "switchlevel" => new[] { "level" },
            "mediaplayback" => new[] { "playbackStatus" },
            "mediatrackcontrol" => Array.Empty<string>(),
            "audiovolume" => new[] { "volume" },
            "audiomute" => new[] { "mute" },
            _ => Array.Empty<string>()
        };

        var isWritable = normalizedCapabilityId switch
        {
            "switch" or "switchlevel" or "mediaplayback" or "mediatrackcontrol" or "audiovolume" or "audiomute" => true,
            _ => false
        };

        return new CapabilitySummary(capabilityId, version, commands, attributes, true, isWritable);
    }

    private static Exception CreateException(HttpStatusCode statusCode, string? responseContent)
    {
        var message = string.IsNullOrWhiteSpace(responseContent)
            ? $"SmartThings API request failed with status {(int)statusCode}."
            : $"SmartThings API request failed with status {(int)statusCode}: {responseContent}";

        return statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new SmartThingsAuthException(message),
            HttpStatusCode.TooManyRequests => new SmartThingsRateLimitException(message),
            HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout or HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable
                => new SmartThingsTransientException(message),
            _ => new SmartThingsException(message)
        };
    }
}
