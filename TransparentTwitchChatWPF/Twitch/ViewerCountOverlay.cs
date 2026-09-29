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
        window.ttcoFormatActivityAge = function (iso) {
          const then = Date.parse(iso);
          if (!iso || !isFinite(then)) return '';
          const minutes = Math.floor((Date.now() - then) / 60000);
          if (minutes < 1) return 'just now';
          if (minutes < 60) return minutes === 1 ? '1 minute ago' : minutes + ' minutes ago';
          const hours = Math.floor(minutes / 60);
          if (hours < 24) return hours === 1 ? '1 hour ago' : hours + ' hours ago';
          const days = Math.floor(hours / 24);
          if (days < 30) return days === 1 ? '1 day ago' : days + ' days ago';
          const months = Math.floor(days / 30);
          if (months < 12) return months === 1 ? '1 month ago' : months + ' months ago';
          const years = Math.floor(days / 365);
          return years === 1 ? '1 year ago' : years + ' years ago';
        };

        window.ttcoActivityIcons = {
          follow: '<svg viewBox="0 0 20 20" aria-hidden="true"><path fill="#FF4F8B" d="M10 17.2S4.2 13.6 2.4 10.8C1 8.6 1.7 5.4 4.4 4.5c1.6-.5 3.2.1 4.1 1.4.9-1.3 2.5-1.9 4.1-1.4 2.7.9 3.4 4.1 2 6.3-1.8 2.8-7.6 6.4-7.6 6.4z"/></svg>',
          subscribe: '<svg viewBox="0 0 20 20" aria-hidden="true"><path fill="#9146FF" d="m10 1.7 2.2 4.6 5 .7-3.6 3.5.8 5L10 13.1l-4.4 2.4.8-5L2.8 7l5-.7L10 1.7z"/></svg>',
          resub: '<svg viewBox="0 0 20 20" aria-hidden="true"><path fill="#9146FF" d="m10 1.7 2.2 4.6 5 .7-3.6 3.5.8 5L10 13.1l-4.4 2.4.8-5L2.8 7l5-.7L10 1.7z"/></svg>',
          gift: '<svg viewBox="0 0 20 20" aria-hidden="true"><path fill="#9146FF" d="M10 4.2c.6-1.4 2.6-1.6 3.4-.6.5.6.4 1.5-.2 2.1H16v3H4V5.7h2.8c-.6-.6-.7-1.5-.2-2.1.8-1 2.8-.8 3.4.6zM4 11h5.2v6H5.4A1.4 1.4 0 0 1 4 15.6V11zm6.8 0H16v4.6a1.4 1.4 0 0 1-1.4 1.4h-3.8V11z"/></svg>',
          cheer: '<svg viewBox="0 0 20 20" aria-hidden="true"><path fill="#BF94FF" d="M10 1.6 16.2 7 10 18.4 3.8 7 10 1.6zm0 2.6L6.2 7h7.6L10 4.2zM5.4 8.4 10 16.2l4.6-7.8H5.4z"/></svg>',
          raid: '<svg viewBox="0 0 20 20" aria-hidden="true"><path fill="#00A3FF" d="M3.2 6.2 8.8 10 3.2 13.8l1.5 1.7L12.2 10 4.7 4.5 3.2 6.2zm5.2 0L14 10l-5.6 3.8 1.5 1.7L17.4 10 9.9 4.5 8.4 6.2z"/></svg>',
          redeem: '<svg viewBox="0 0 20 20" aria-hidden="true"><path fill="#9146FF" d="M10 1.4 16.8 5.2v7.6L10 16.6 3.2 12.8V5.2L10 1.4zm0 2.3L5.2 6.4v5.2L10 14.3l4.8-2.7V6.4L10 3.7z"/></svg>'
        };

        window.ttcoRenderActivity = function () {
          const icon = document.getElementById('ttco-viewer-count-activity-icon');
          const label = document.getElementById('ttco-viewer-count-activity-text');
          if (!label) return;
          const text = window.ttcoActivityText || '';
          const age = window.ttcoFormatActivityAge(window.ttcoActivityAt);
          label.textContent = text ? (age ? text + ' · ' + age : text) : '';
          if (icon) {
            const markup = text ? (window.ttcoActivityIcons[window.ttcoActivityKind] || '') : '';
            icon.innerHTML = markup;
            icon.style.display = markup ? 'inline-flex' : 'none';
          }
        };

        if (!window.ttcoActivityTimer) {
          window.ttcoActivityTimer = setInterval(function () { window.ttcoRenderActivity(); }, 30000);
        }

        window.ttcoEnsureViewerCount = function () {
          let el = document.getElementById('ttco-viewer-count');
          if (!el) {
            el = document.createElement('div');
            el.id = 'ttco-viewer-count';
            (document.body || document.documentElement).appendChild(el);
          }

          let style = document.getElementById('ttco-viewer-count-style');
          if (!style) {
            style = document.createElement('style');
            style.id = 'ttco-viewer-count-style';
            (document.head || document.documentElement).appendChild(style);
          }
          style.textContent = '#ttco-viewer-count{position:fixed;top:0.35em;left:0.45em;right:0.45em;z-index:2147483647;pointer-events:none;display:flex;align-items:center;gap:0.75em;line-height:1;background:transparent;color:#fff;}#ttco-viewer-count-count{display:flex;align-items:center;gap:0.35em;flex:none;margin-left:auto;}#ttco-viewer-count-dot{width:0.45em;height:0.45em;border-radius:50%;background:#e91916;flex:none;}#ttco-viewer-count.is-offline #ttco-viewer-count-dot{background:#888;}#ttco-viewer-count-text{flex:none;}#ttco-viewer-count-activity{display:flex;align-items:center;gap:0.35em;flex:1 1 auto;min-width:0;overflow:hidden;}#ttco-viewer-count-activity-icon{flex:none;width:1em;height:1em;display:inline-flex;align-items:center;justify-content:center;}#ttco-viewer-count-activity-icon svg{width:1em;height:1em;display:block;}#ttco-viewer-count-activity-text{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;min-width:0;}';

          let activity = document.getElementById('ttco-viewer-count-activity');
          if (!activity) {
            activity = document.createElement('span');
            activity.id = 'ttco-viewer-count-activity';
          }
          let activityIcon = document.getElementById('ttco-viewer-count-activity-icon');
          if (!activityIcon) {
            activityIcon = document.createElement('span');
            activityIcon.id = 'ttco-viewer-count-activity-icon';
          }
          let activityText = document.getElementById('ttco-viewer-count-activity-text');
          if (!activityText) {
            activityText = document.createElement('span');
            activityText.id = 'ttco-viewer-count-activity-text';
            activityText.className = 'text-content';
          }
          activity.appendChild(activityIcon);
          activity.appendChild(activityText);
          let count = document.getElementById('ttco-viewer-count-count');
          if (!count) {
            count = document.createElement('span');
            count.id = 'ttco-viewer-count-count';
          }
          let dot = document.getElementById('ttco-viewer-count-dot');
          if (!dot) {
            dot = document.createElement('span');
            dot.id = 'ttco-viewer-count-dot';
          }
          let label = document.getElementById('ttco-viewer-count-text');
          if (!label) {
            label = document.createElement('span');
            label.id = 'ttco-viewer-count-text';
            label.className = 'text-content';
          }
          count.appendChild(dot);
          count.appendChild(label);
          el.appendChild(activity);
          el.appendChild(count);

          const source =
            document.getElementById('chat_container') ||
            document.querySelector('.chat_line') ||
            document.querySelector('.chat-line__message') ||
            document.getElementById('chat_box');

          if (source) {
            const computed = getComputedStyle(source);
            el.style.fontFamily = computed.fontFamily;
            const scale = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--scale'));
            const base = parseFloat(computed.fontSize);
            const s = scale > 0 ? scale : 1;
            el.style.fontSize = (base > 0 ? (base * s) + 'px' : computed.fontSize);
            el.style.fontWeight = computed.fontWeight;
            el.style.fontStyle = computed.fontStyle;
            el.style.fontVariant = computed.fontVariant;
            el.style.color = computed.color;
            el.style.letterSpacing = computed.letterSpacing;
            el.style.transform = 'none';
            if (computed.filter && computed.filter !== 'none') {
              el.style.filter = computed.filter;
            }
            ['ttco-viewer-count-text', 'ttco-viewer-count-activity-text'].forEach(function (id) {
              const node = document.getElementById(id);
              if (node && computed.textShadow && computed.textShadow !== 'none') {
                node.style.textShadow = computed.textShadow;
              }
            });
          }

          if (!window.ttcoStyleWatchBound) {
            window.ttcoStyleWatchBound = true;
            const sync = function () {
              if (typeof window.ttcoEnsureViewerCount === 'function') window.ttcoEnsureViewerCount();
            };
            const hookLink = function (node) {
              if (!node || node.tagName !== 'LINK') return;
              node.addEventListener('load', sync, { once: true });
            };
            document.querySelectorAll('link[rel="stylesheet"]').forEach(hookLink);
            const head = document.head || document.documentElement;
            new MutationObserver(function (records) {
              records.forEach(function (record) {
                record.addedNodes.forEach(hookLink);
              });
            }).observe(head, { childList: true });
            if (document.fonts) {
              document.fonts.ready.then(sync);
              document.fonts.addEventListener('loadingdone', sync);
            }
            new MutationObserver(sync).observe(document.documentElement, { attributes: true, attributeFilter: ['style'] });
            const watchContainer = function () {
              const container = document.getElementById('chat_container');
              if (!container || container.ttcoStyleObserved) return;
              container.ttcoStyleObserved = true;
              new MutationObserver(sync).observe(container, { attributes: true, attributeFilter: ['style', 'class'] });
              sync();
            };
            watchContainer();
            [100, 400, 1000, 2000].forEach(function (ms) {
              setTimeout(function () { watchContainer(); sync(); }, ms);
            });
          }

          return el;
        };

        window.ttcoSetViewerCount = function (text, state, activity, activityAt, activityKind) {
          const el = window.ttcoEnsureViewerCount();
          const showCount = text != null;
          const showActivity = activity != null && activity !== '';
          const count = document.getElementById('ttco-viewer-count-count');
          const activityEl = document.getElementById('ttco-viewer-count-activity');
          if (!showCount && !showActivity) {
            el.style.display = 'none';
            return;
          }
          el.style.display = 'flex';
          if (count) count.style.display = showCount ? 'flex' : 'none';
          if (activityEl) activityEl.style.display = showActivity ? 'flex' : 'none';
          el.classList.toggle('is-offline', state === 'offline');
          el.classList.toggle('is-live', state === 'live');
          const label = document.getElementById('ttco-viewer-count-text');
          if (label && showCount) label.textContent = text;
          window.ttcoActivityText = showActivity ? activity : '';
          window.ttcoActivityAt = showActivity ? (activityAt || '') : '';
          window.ttcoActivityKind = showActivity ? (activityKind || '') : '';
          window.ttcoRenderActivity();
        };
        """;

    public static async Task UpdateAsync(CoreWebView2 core, string text, string state, string activity, DateTimeOffset? activityAt, string activityKind)
    {
        if (core == null)
            return;

        string textArg = text == null ? "null" : JsonSerializer.Serialize(text);
        string stateArg = JsonSerializer.Serialize(state ?? "hidden");
        string activityArg = activity == null ? "null" : JsonSerializer.Serialize(activity);
        string activityAtArg = activityAt.HasValue
            ? JsonSerializer.Serialize(activityAt.Value.UtcDateTime.ToString("o"))
            : "null";
        string kindArg = activityKind == null ? "null" : JsonSerializer.Serialize(activityKind);
        try
        {
            await core.ExecuteScriptAsync(InstallScript + $"window.ttcoSetViewerCount({textArg}, {stateArg}, {activityArg}, {activityAtArg}, {kindArg});");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Viewer count overlay update failed: {ex.Message}");
        }
    }
}
