using System.Diagnostics;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace TransparentTwitchChatWPF.Twitch;

/// <summary>
/// Draws the viewer count inside the chat page so it uses the same font, size, weight, outline, and shadow as the chat.
/// </summary>
public static class ViewerCountOverlay
{
    private const string InstallScript = """
        window.ttcoEnsureViewerCount = function () {
          let el = document.getElementById('ttco-viewer-count');
          if (!el) {
            el = document.createElement('div');
            el.id = 'ttco-viewer-count';
            el.innerHTML = '<span id="ttco-viewer-count-dot"></span><span id="ttco-viewer-count-text" class="text-content"></span>';
            (document.body || document.documentElement).appendChild(el);

            let style = document.getElementById('ttco-viewer-count-style');
            if (!style) {
              style = document.createElement('style');
              style.id = 'ttco-viewer-count-style';
              style.textContent = '#ttco-viewer-count{position:fixed;top:0.35em;right:0.45em;z-index:2147483647;pointer-events:none;display:flex;align-items:center;gap:0.35em;line-height:1;transform-origin:top right;background:transparent;color:#fff;}#ttco-viewer-count-dot{width:0.45em;height:0.45em;border-radius:50%;background:#e91916;flex:none;}#ttco-viewer-count.is-offline #ttco-viewer-count-dot{background:#888;}';
              (document.head || document.documentElement).appendChild(style);
            }
          }

          const source =
            document.getElementById('chat_container') ||
            document.querySelector('.chat_line') ||
            document.querySelector('.chat-line__message') ||
            document.getElementById('chat_box');

          if (source) {
            const computed = getComputedStyle(source);
            el.style.fontFamily = computed.fontFamily;
            el.style.fontSize = computed.fontSize;
            el.style.fontWeight = computed.fontWeight;
            el.style.fontStyle = computed.fontStyle;
            el.style.fontVariant = computed.fontVariant;
            el.style.color = computed.color;
            el.style.letterSpacing = computed.letterSpacing;
            if (computed.filter && computed.filter !== 'none') {
              el.style.filter = computed.filter;
            }
            const label = document.getElementById('ttco-viewer-count-text');
            if (label && computed.textShadow && computed.textShadow !== 'none') {
              label.style.textShadow = computed.textShadow;
            }
          }

          const scale = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--scale'));
          el.style.transform = 'scale(' + (scale > 0 ? scale : 1) + ')';
          return el;
        };

        window.ttcoSetViewerCount = function (text, state) {
          const el = window.ttcoEnsureViewerCount();
          if (text == null) {
            el.style.display = 'none';
            return;
          }
          el.style.display = 'flex';
          el.classList.toggle('is-offline', state === 'offline');
          el.classList.toggle('is-live', state === 'live');
          const label = document.getElementById('ttco-viewer-count-text');
          if (label) label.textContent = text;
        };
        """;

    public static async Task UpdateAsync(CoreWebView2 core, string text, string state)
    {
        if (core == null)
            return;

        string textArg = text == null ? "null" : JsonSerializer.Serialize(text);
        string stateArg = JsonSerializer.Serialize(state ?? "hidden");
        try
        {
            await core.ExecuteScriptAsync(InstallScript + $"window.ttcoSetViewerCount({textArg}, {stateArg});");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Viewer count overlay update failed: {ex.Message}");
        }
    }
}
