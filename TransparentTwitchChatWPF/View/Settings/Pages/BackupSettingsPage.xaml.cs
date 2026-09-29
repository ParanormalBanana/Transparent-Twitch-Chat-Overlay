using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace TransparentTwitchChatWPF.View.Settings;

public partial class BackupSettingsPage : UserControl
{
    public BackupSettingsPage()
    {
        InitializeComponent();
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Settings backup (*.zip)|*.zip",
            FileName = "TransparentTwitchChat-settings.zip",
            Title = "Export settings"
        };
        if (dialog.ShowDialog() != true)
            return;

        string temp = Path.Combine(Path.GetTempPath(), "ttco-export-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(temp);
            string folder = App.Settings.UserDataFolder;
            if (Directory.Exists(folder))
            {
                foreach (string file in Directory.GetFiles(folder, "*.json"))
                    File.Copy(file, Path.Combine(temp, Path.GetFileName(file)), true);
            }

            if (Directory.Exists(ChatSounds.CustomFolder))
            {
                string sounds = Path.Combine(temp, "sounds");
                Directory.CreateDirectory(sounds);
                foreach (string file in Directory.GetFiles(ChatSounds.CustomFolder))
                    File.Copy(file, Path.Combine(sounds, Path.GetFileName(file)), true);
            }

            if (File.Exists(dialog.FileName))
                File.Delete(dialog.FileName);
            ZipFile.CreateFromDirectory(temp, dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this), ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            try { if (Directory.Exists(temp)) Directory.Delete(temp, true); } catch { }
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Settings backup (*.zip)|*.zip",
            Title = "Import settings"
        };
        if (dialog.ShowDialog() != true)
            return;

        if (MessageBox.Show(Window.GetWindow(this),
                "Replace your current settings and restart?",
                "Import settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        string pending = Path.Combine(App.Settings.UserDataFolder, "import-pending");
        try
        {
            if (Directory.Exists(pending))
                Directory.Delete(pending, true);
            Directory.CreateDirectory(pending);

            using var zip = ZipFile.OpenRead(dialog.FileName);
            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                string relative = entry.FullName.Replace('\\', '/');
                string dest;
                if (relative.StartsWith("sounds/", StringComparison.OrdinalIgnoreCase))
                {
                    if (!IsSoundFile(entry.Name))
                        continue;
                    string sounds = Path.Combine(pending, "sounds");
                    Directory.CreateDirectory(sounds);
                    dest = Path.Combine(sounds, entry.Name);
                }
                else if (!relative.Contains('/') && entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    dest = Path.Combine(pending, entry.Name);
                }
                else
                {
                    continue;
                }

                string full = Path.GetFullPath(dest);
                string root = Path.GetFullPath(pending) + Path.DirectorySeparatorChar;
                if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    continue;

                entry.ExtractToFile(full, true);
            }

            if (!Directory.EnumerateFiles(pending, "*.json").Any())
            {
                Directory.Delete(pending, true);
                MessageBox.Show(Window.GetWindow(this), "That file has no settings.", "Import failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        catch (Exception ex)
        {
            try { if (Directory.Exists(pending)) Directory.Delete(pending, true); } catch { }
            MessageBox.Show(Window.GetWindow(this), ex.Message, "Import failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        string exe = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exe))
            Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
        Application.Current.Shutdown();
    }

    private static bool IsSoundFile(string name)
    {
        return name.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase);
    }
}
