using System.Net.Http.Headers;
using IchigoHoshimiya.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IchigoHoshimiya.Services;

public class HetznerSettings
{
    public string ApiToken { get; set; } = string.Empty;
    public long ServerId { get; set; }
}

public class HetznerService(
    HttpClient httpClient,
    IOptions<HetznerSettings> options,
    ILogger<HetznerService> logger)
    : IHetznerService
{
    private readonly HetznerSettings _settings = options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.ApiToken) && _settings.ServerId > 0;

    public Task StartServerAsync(CancellationToken cancellationToken = default) =>
        PostActionAsync("poweron", cancellationToken);

    public Task StopServerAsync(CancellationToken cancellationToken = default) =>
        PostActionAsync("shutdown", cancellationToken);

    private async Task PostActionAsync(string action, CancellationToken cancellationToken)
    {
        var path = $"/v1/servers/{_settings.ServerId}/actions/{action}";
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiToken);
        // Hetzner accepts an empty body for these actions but expects a JSON content type.
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Hetzner {Action} failed (server={ServerId}): {Status} {Body}",
                action, _settings.ServerId, (int)response.StatusCode, body);
            throw new HttpRequestException(
                $"Hetzner API returned {(int)response.StatusCode}: {body}");
        }

        logger.LogInformation(
            "Hetzner {Action} accepted (server={ServerId}): {Body}",
            action, _settings.ServerId, body);
    }
}
