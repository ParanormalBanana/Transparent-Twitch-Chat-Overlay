using System.IO;

namespace TransparentTwitchChatWPF;

public static class ChatSounds
{
    public const string CustomOption = "Custom";

    public static string CustomFolder => Path.Combine(App.Settings.UserDataFolder, "sounds");

    public static string BundledFolder()
    {
        string path = App.Settings.GeneralSettings.SoundClipsFolder;
        string fallback = Path.Combine(AppContext.BaseDirectory, "assets");
        if (string.IsNullOrWhiteSpace(path) || path == "Default" || !Directory.Exists(path))
            path = fallback;
        return path;
    }

    public static string Resolve(string name)
    {
        if (string.IsNullOrWhiteSpace(name)
            || name.Equals("none", StringComparison.OrdinalIgnoreCase)
            || name.Equals(CustomOption, StringComparison.OrdinalIgnoreCase))
            return null;

        if (Path.IsPathRooted(name) && File.Exists(name))
            return name;

        string custom = Path.Combine(CustomFolder, name);
        if (File.Exists(custom))
            return custom;

        string bundled = Path.Combine(BundledFolder(), name);
        return File.Exists(bundled) ? bundled : null;
    }

    public static IEnumerable<string> List()
    {
        var names = new List<string>();
        AddFiles(names, BundledFolder());
        AddFiles(names, CustomFolder);
        return names;
    }

    public static string Import(string sourcePath)
    {
        Directory.CreateDirectory(CustomFolder);
        string name = Path.GetFileName(sourcePath);
        string dest = Path.Combine(CustomFolder, name);
        if (!string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
            File.Copy(sourcePath, dest, true);
        return name;
    }

    private static void AddFiles(List<string> names, string folder)
    {
        if (!Directory.Exists(folder))
            return;

        foreach (string pattern in new[] { "*.wav", "*.mp3" })
        {
            foreach (string file in Directory.GetFiles(folder, pattern))
            {
                string name = Path.GetFileName(file);
                if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
                    names.Add(name);
            }
        }
    }
}
