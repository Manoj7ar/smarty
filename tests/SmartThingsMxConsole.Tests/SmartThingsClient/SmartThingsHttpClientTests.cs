using System.Net;
using System.Text;
using System.Text.Json;
using SmartThingsClient;
using SmartThingsClient.Configuration;
using SmartThingsClient.Internal;
using SmartThingsMxConsole.Core.Exceptions;
using SmartThingsMxConsole.Core.Models;

namespace SmartThingsMxConsole.Tests.SmartThingsClient;

public sealed class SmartThingsHttpClientTests
{
    [Fact]
    public async Task ListScenesAsync_MapsItemsFromPayload()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "items": [
                        { "sceneId": "scene-1", "sceneName": "Movie Time", "locationId": "loc-1" },
                        { "sceneId": "scene-2", "sceneName": "Good Night", "locationId": "loc-1" }
                      ]
                    }
                    """, Encoding.UTF8, "application/json")
            }));

        var client = CreateClient(handler);

        var scenes = await client.ListScenesAsync();

        Assert.Collection(
            scenes,
            first =>
            {
                Assert.Equal("scene-1", first.Id);
                Assert.Equal("Movie Time", first.Name);
            },
            second =>
            {
                Assert.Equal("scene-2", second.Id);
                Assert.Equal("Good Night", second.Name);
            });
    }

    [Fact]
    public async Task ListDevicesAsync_FlattensCapabilitiesAcrossComponents()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "items": [
                        {
                          "deviceId": "device-1",
                          "name": "Living Room TV",
                          "label": "TV",
                          "manufacturerName": "Samsung",
                          "components": [
                            {
                              "id": "main",
                              "capabilities": [
                                { "id": "switch", "version": 1 },
                                { "id": "mediaPlayback", "version": 1 }
                              ]
                            },
                            {
                              "id": "speaker",
                              "capabilities": [
                                { "id": "audioVolume", "version": 1 }
                              ]
                            }
                          ]
                        }
                      ]
                    }
                    """, Encoding.UTF8, "application/json")
            }));

        var client = CreateClient(handler);

        var devices = await client.ListDevicesAsync();

        var device = Assert.Single(devices);
        Assert.Equal("device-1", device.Id);
        Assert.Equal(3, device.Capabilities.Count);
        Assert.Contains(device.Capabilities, capability => capability.CapabilityId == "switch");
        Assert.Contains(device.Capabilities, capability => capability.CapabilityId == "mediaPlayback");
        Assert.Contains(device.Capabilities, capability => capability.CapabilityId == "audioVolume");
    }

    [Fact]
    public async Task GetDeviceStatusAsync_ParsesDynamicCapabilityPayload()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "components": {
                        "main": {
                          "switch": {
                            "attributes": {
                              "switch": {
                                "value": "on",
                                "timestamp": "2026-03-11T12:00:00Z"
                              }
                            }
                          },
                          "switchLevel": {
                            "attributes": {
                              "level": {
                                "value": 62,
                                "unit": "%",
                                "data": { "range": [0, 100] }
                              }
                            }
                          }
                        }
                      }
                    }
                    """, Encoding.UTF8, "application/json")
            }));

        var client = CreateClient(handler);

        var status = await client.GetDeviceStatusAsync("device-1");

        Assert.Equal("device-1", status.DeviceId);
        Assert.True(status.TryGetString("switch", "switch", out var switchValue));
        Assert.Equal("on", switchValue);
        Assert.True(status.TryGetDouble("switchLevel", "level", out var level));
        Assert.Equal(62, level);
    }

    [Fact]
    public async Task SendDeviceCommandAsync_SendsExpectedPayloadAndBearerToken()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedPayload = null;
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            capturedRequest = request;
            capturedPayload = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });

        var client = CreateClient(handler);

        await client.SendDeviceCommandAsync(
            "device-1",
            new DeviceCommandRequest("speaker", "audioVolume", "setVolume", [25]));

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal("pat-token", capturedRequest.Headers.Authorization?.Parameter);

        Assert.NotNull(capturedPayload);
        using var document = JsonDocument.Parse(capturedPayload);
        var command = document.RootElement.GetProperty("commands")[0];
        Assert.Equal("speaker", command.GetProperty("component").GetString());
        Assert.Equal("audioVolume", command.GetProperty("capability").GetString());
        Assert.Equal(25, command.GetProperty("arguments")[0].GetInt32());
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, typeof(SmartThingsAuthException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(SmartThingsAuthException))]
    [InlineData(HttpStatusCode.TooManyRequests, typeof(SmartThingsRateLimitException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(SmartThingsTransientException))]
    public async Task ListScenesAsync_MapsHttpErrorsToTypedExceptions(HttpStatusCode statusCode, Type exceptionType)
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("{\"error\":\"boom\"}", Encoding.UTF8, "application/json")
            }));

        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync(exceptionType, () => client.ListScenesAsync());
        Assert.Contains(((int)statusCode).ToString(), ((Exception)exception).Message);
    }

    [Fact]
    public async Task ListScenesAsync_ThrowsAuthenticationException_WhenTokenMissing()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new SmartThingsHttpClient(
            new HttpClient(handler),
            new StaticTokenProvider(null),
            SmartThingsClientOptions.Default);

        await Assert.ThrowsAsync<SmartThingsAuthException>(() => client.ListScenesAsync());
    }

    private static SmartThingsHttpClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new StaticTokenProvider("pat-token"), SmartThingsClientOptions.Default);

    private sealed class StaticTokenProvider(string? token) : IBearerTokenProvider
    {
        public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(token);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }
}
