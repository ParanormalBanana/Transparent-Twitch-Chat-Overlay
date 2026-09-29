using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TransparentTwitchChatWPF.Twitch;
using TransparentTwitchChatWPF.Utils;

namespace TransparentTwitchChatWPF.View.Settings;

/// <summary>
/// Interaction logic for WidgesSettingsPage.xaml
/// </summary>
public partial class WidgetSettingsPage : UserControl
{
    public event Action WidgetCreationRequested;
    public event Action<string, string> DefaultWidgetRequested;
    public event Action<string> RemoveWidgetRequested;
    public event Action<string> EditWidgetRequested;
    public event Action<string, bool> WidgetEnabledRequested;
    public Func<string, string> DisplayNameLookup;

    public WidgetSettingsPage()
    {
        InitializeComponent();

        foreach (var preset in TwitchWidgets.Defaults)
            DefaultWidgetCombo.Items.Add(new ComboBoxItem { Content = preset.Name, Tag = preset });
        if (DefaultWidgetCombo.Items.Count > 0)
            DefaultWidgetCombo.SelectedIndex = 0;
    }

    public void Refresh()
    {
        CreatedWidgetsPanel.Children.Clear();
        var widgets = App.Settings?.GeneralSettings?.CustomWindows;
        bool any = widgets != null && widgets.Count > 0;
        NoWidgetsText.Visibility = any ? Visibility.Collapsed : Visibility.Visible;
        if (!any)
            return;

        foreach (string url in widgets)
        {
            var row = new DockPanel { Margin = new Thickness(0, 4, 0, 0), LastChildFill = true };
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            bool enabled = !IsWidgetDisabled(url);
            var toggle = new Button
            {
                Content = enabled ? "Disable" : "Enable",
                MinWidth = 96,
                MinHeight = 32,
                Padding = new Thickness(14, 4, 14, 4),
                Tag = url,
                Margin = new Thickness(8, 0, 0, 0)
            };
            toggle.Click += ToggleWidget_Click;
            var edit = new Button
            {
                Content = "Edit",
                MinWidth = 72,
                MinHeight = 32,
                Padding = new Thickness(14, 4, 14, 4),
                Tag = url,
                Margin = new Thickness(8, 0, 0, 0)
            };
            edit.Click += EditWidget_Click;
            var remove = new Button
            {
                Content = new TextBlock
                {
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    Text = "\uE74D",
                    Foreground = new SolidColorBrush(Color.FromRgb(232, 17, 35)),
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                Width = 36,
                MinHeight = 32,
                Padding = new Thickness(4),
                ToolTip = "Delete",
                Tag = url,
                Margin = new Thickness(8, 0, 0, 0)
            };
            remove.Click += RemoveWidget_Click;
            buttons.Children.Add(toggle);
            buttons.Children.Add(edit);
            buttons.Children.Add(remove);
            DockPanel.SetDock(buttons, Dock.Right);

            string lookedUp = DisplayNameLookup?.Invoke(url);
            var label = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(lookedUp) ? TwitchWidgets.DisplayNameFor(url) : lookedUp,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = url
            };
            row.Children.Add(buttons);
            row.Children.Add(label);
            CreatedWidgetsPanel.Children.Add(row);
        }
    }

    private void AddDefaultWidget_Click(object sender, RoutedEventArgs e)
    {
        if (DefaultWidgetCombo.SelectedItem is not ComboBoxItem item || item.Tag is not TwitchWidgetPreset preset)
            return;

        var login = ViewerCountClient.ResolveChannelLogin();
        if (string.IsNullOrWhiteSpace(login))
        {
            MessageBox.Show(Window.GetWindow(this),
                "Set the channel in Chat settings before adding a default widget.",
                "Channel needed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(App.Settings.GeneralSettings.OAuthToken))
        {
            MessageBox.Show(Window.GetWindow(this),
                "Connect Twitch in Connections so this widget can use that login.",
                "Twitch connection needed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        string url = TwitchWidgets.UrlFor(preset, login);
        if (App.Settings.GeneralSettings.CustomWindows.Contains(url))
        {
            MessageBox.Show(Window.GetWindow(this),
                "That widget is already in the list.",
                "Widget exists",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        DefaultWidgetRequested?.Invoke(url, preset.Name);
        Refresh();
    }

    private void RemoveWidget_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string url)
            return;

        if (MessageBox.Show(Window.GetWindow(this),
                "Delete this widget and its saved settings?",
                "Delete widget",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        RemoveWidgetRequested?.Invoke(url);
        Refresh();
    }

    private void ToggleWidget_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string url)
            return;

        bool enabled = !IsWidgetDisabled(url);
        WidgetEnabledRequested?.Invoke(url, !enabled);
        Refresh();
    }

    private static bool IsWidgetDisabled(string url)
    {
        var disabled = App.Settings?.GeneralSettings?.DisabledWidgets;
        return disabled != null && disabled.Contains(url);
    }

    private void EditWidget_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string url)
            return;

        EditWidgetRequested?.Invoke(url);
        Refresh();
    }

    private void NewWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        WidgetCreationRequested?.Invoke();
        Refresh();
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        ShellHelper.OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }
}
