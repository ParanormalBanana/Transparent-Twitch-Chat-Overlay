using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace TransparentTwitchChatWPF.Twitch;

public readonly record struct TwitchWidgetPreset(string Name, string Slug);

public static class TwitchWidgets
{
    public static IReadOnlyList<TwitchWidgetPreset> Defaults { get; } = new[]
    {
        new TwitchWidgetPreset("Stream Preview", "stream-preview"),
        new TwitchWidgetPreset("Quick Actions", "quick-actions"),
    };

    public static string UrlFor(TwitchWidgetPreset preset, string login)
    {
        if (string.Equals(preset.Slug, "stream-preview", StringComparison.OrdinalIgnoreCase))
            return PlayerPopoutUrl(login);

        return $"https://dashboard.twitch.tv/popout/u/{Uri.EscapeDataString(login)}/stream-manager/{preset.Slug}";
    }

    public static string PlayerPopoutUrl(string login)
    {
        return $"https://player.twitch.tv/?channel={Uri.EscapeDataString(login)}&enableExtensions=true&muted=false&parent=twitch.tv&player=popout&volume=0";
    }

    public static bool TryAddDefaultPreviewVolume(string url, out string updated)
    {
        updated = null;
        if (string.IsNullOrWhiteSpace(url)
            || !url.Contains("player.twitch.tv", StringComparison.OrdinalIgnoreCase)
            || !url.Contains("player=popout", StringComparison.OrdinalIgnoreCase)
            || url.Contains("volume=", StringComparison.OrdinalIgnoreCase)
            || !Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            return false;

        var keys = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=')[0])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        string[] expected = { "channel", "enableExtensions", "muted", "parent", "player" };
        if (keys.Count != expected.Length || expected.Any(key => !keys.Contains(key)))
            return false;

        updated = url + "&volume=0";
        return true;
    }

    public static bool TryGetDashboardStreamPreviewLogin(string url, out string login)
    {
        login = null;
        if (string.IsNullOrWhiteSpace(url)
            || !url.Contains("/stream-manager/stream-preview", StringComparison.OrdinalIgnoreCase))
            return false;

        const string marker = "/popout/u/";
        int start = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return false;

        string rest = url[(start + marker.Length)..];
        int slash = rest.IndexOf('/');
        if (slash <= 0)
            return false;

        login = Uri.UnescapeDataString(rest[..slash]);
        return !string.IsNullOrWhiteSpace(login);
    }

    public static string DisplayNameFor(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "Widget";

        if (url.Contains("player.twitch.tv", StringComparison.OrdinalIgnoreCase)
            && url.Contains("player=popout", StringComparison.OrdinalIgnoreCase))
            return "Stream Preview";

        foreach (var preset in Defaults)
        {
            if (url.Contains("/stream-manager/" + preset.Slug, StringComparison.OrdinalIgnoreCase))
                return preset.Name;
        }

        return url;
    }

    public static bool IsDashboardWidget(string url)
    {
        return !string.IsNullOrWhiteSpace(url)
            && url.Contains("dashboard.twitch.tv/popout/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsQuickActionsWidget(string url)
    {
        return !string.IsNullOrWhiteSpace(url)
            && url.Contains("/stream-manager/quick-actions", StringComparison.OrdinalIgnoreCase);
    }

    public static string DefaultCssFor(string url)
    {
        if (IsQuickActionsWidget(url))
            return QuickActionsCss;
        if (IsDashboardWidget(url))
            return PopoutFitCss;
        return "";
    }

    public const string PopoutFitCss = """
        .scrollable-area.sunlight-page__content {
          padding-bottom: 30px !important;
        }
        """;

    public const string QuickActionsCss = """
        .scrollable-area.sunlight-page__content {
          padding-bottom: 30px !important;
        }

        .stream-manager-panel-header {
          display: none !important;
        }
        """;
}

public static class TwitchWebSession
{
    public static void ApplyConnectedAccount(CoreWebView2 core)
    {
        var token = App.Settings?.GeneralSettings?.OAuthToken;
        if (core == null || string.IsNullOrWhiteSpace(token))
            return;

        var cookie = core.CookieManager.CreateCookie("auth-token", token, ".twitch.tv", "/");
        cookie.IsSecure = true;
        cookie.Expires = DateTime.UtcNow.AddDays(30);
        core.CookieManager.AddOrUpdateCookie(cookie);
    }

    public static async Task PrepareDashboardPageAsync(CoreWebView2 core, string login)
    {
        if (core == null || string.IsNullOrWhiteSpace(login))
            return;

        string referrer = JsonSerializer.Serialize($"https://www.twitch.tv/{login}/dashboard/live");
        string script = $$"""
            try { Object.defineProperty(document, 'referrer', { get: function () { return {{referrer}}; } }); } catch (e) {}
            try { localStorage.setItem('twilight.theme', '1'); } catch (e) {}
            """;
        await core.AddScriptToExecuteOnDocumentCreatedAsync(script);
    }
}
