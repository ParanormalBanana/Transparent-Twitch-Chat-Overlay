using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using TransparentTwitchChatWPF;

namespace TransparentTwitchChatWPF.Twitch;

public enum ViewerCountState
{
    Live,
    Offline,
    Unavailable
}

public readonly record struct ViewerCountSnapshot(ViewerCountState State, int Viewers);

/// <summary>
/// Reads the live viewer count for a Twitch channel.
/// Uses Helix when the app has an OAuth token, and ivr.fi otherwise.
/// </summary>
public static class ViewerCountClient
{
    private const string ClientId = "yv4bdnndvd4gwsfw7jnfixp0mnofn7";

    private static readonly HttpClient Http = CreateClient();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string ResolveChannelLogin()
    {
        var settings = App.Settings?.GeneralSettings;
        if (settings == null)
            return null;

        if (settings.ChatType == (int)ChatTypes.CustomURL
            && !string.IsNullOrWhiteSpace(settings.CustomURL)
            && settings.CustomURL.Contains("twitch.tv", StringComparison.OrdinalIgnoreCase))
        {
            var fromUrl = NormalizeLogin(settings.CustomURL);
            if (fromUrl != null)
                return fromUrl;
        }

        var login = NormalizeLogin(settings.Username);
        if (login != null)
            return login;

        return NormalizeLogin(App.Settings.jChatSettings?.Channel);
    }

    public static async Task<ViewerCountSnapshot> GetAsync(string login, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(login))
            return new ViewerCountSnapshot(ViewerCountState.Unavailable, 0);

        var token = App.Settings?.GeneralSettings?.OAuthToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            var helix = await TryHelixAsync(login, token, cancellationToken);
            if (helix.HasValue)
                return helix.Value;
        }

        var fallback = await TryIvrAsync(login, cancellationToken);
        if (fallback.HasValue)
            return fallback.Value;

        return new ViewerCountSnapshot(ViewerCountState.Unavailable, 0);
    }

    public static string NormalizeLogin(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var value = raw.Trim();
        if (value.Contains("twitch.tv", StringComparison.OrdinalIgnoreCase))
        {
            var uriText = value.Contains("://", StringComparison.Ordinal) ? value : "https://" + value;
            if (Uri.TryCreate(uriText, UriKind.Absolute, out var uri))
            {
                var segments = uri.AbsolutePath
                    .Trim('/')
                    .Split('/', StringSplitOptions.RemoveEmptyEntries);
                var segment = segments.FirstOrDefault();
                if (segments.Length >= 2
                    && segment != null
                    && (segment.Equals("popout", StringComparison.OrdinalIgnoreCase)
                        || segment.Equals("embed", StringComparison.OrdinalIgnoreCase)
                        || segment.Equals("moderator", StringComparison.OrdinalIgnoreCase)))
                {
                    segment = segments[1];
                }

                if (!string.IsNullOrWhiteSpace(segment))
                    value = segment;
            }
        }
        else if (value.Contains('/'))
        {
            value = value.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? value;
        }

        value = value.Trim().TrimStart('@').ToLowerInvariant();
        if (value.Length == 0 || value.Equals("username", StringComparison.OrdinalIgnoreCase))
            return null;

        foreach (var ch in value)
        {
            if (!char.IsAsciiLetterOrDigit(ch) && ch != '_')
                return null;
        }

        return value;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TransparentTwitchChatOverlay");
        return client;
    }

    private static async Task<ViewerCountSnapshot?> TryHelixAsync(string login, string token, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://api.twitch.tv/helix/streams?user_login=" + Uri.EscapeDataString(login));
            request.Headers.TryAddWithoutValidation("Client-Id", ClientId);
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);

            using var response = await Http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"Helix viewer count failed: {(int)response.StatusCode}");
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<HelixStreamsResponse>(stream, JsonOptions, cancellationToken);
            var live = payload?.Data?.FirstOrDefault();
            if (live == null)
                return new ViewerCountSnapshot(ViewerCountState.Offline, 0);

            return new ViewerCountSnapshot(ViewerCountState.Live, Math.Max(0, live.ViewerCount));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            Debug.WriteLine($"Helix viewer count error: {ex.Message}");
            return null;
        }
    }

    private static async Task<ViewerCountSnapshot?> TryIvrAsync(string login, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Http.GetAsync(
                "https://api.ivr.fi/v2/twitch/user?login=" + Uri.EscapeDataString(login),
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"IVR viewer count failed: {(int)response.StatusCode}");
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var users = await JsonSerializer.DeserializeAsync<List<IvrUser>>(stream, JsonOptions, cancellationToken);
            var user = users?.FirstOrDefault();
            if (user == null)
                return new ViewerCountSnapshot(ViewerCountState.Unavailable, 0);

            if (user.Stream == null)
                return new ViewerCountSnapshot(ViewerCountState.Offline, 0);

            return new ViewerCountSnapshot(ViewerCountState.Live, Math.Max(0, user.Stream.ViewersCount));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            Debug.WriteLine($"IVR viewer count error: {ex.Message}");
            return null;
        }
    }

    private sealed class HelixStreamsResponse
    {
        [JsonPropertyName("data")]
        public List<HelixStream> Data { get; set; }
    }

    private sealed class HelixStream
    {
        [JsonPropertyName("viewer_count")]
        public int ViewerCount { get; set; }
    }

    private sealed class IvrUser
    {
        [JsonPropertyName("stream")]
        public IvrStream Stream { get; set; }
    }

    private sealed class IvrStream
    {
        [JsonPropertyName("viewersCount")]
        public int ViewersCount { get; set; }
    }
}
