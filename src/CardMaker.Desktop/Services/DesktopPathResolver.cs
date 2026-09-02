using System.Runtime.InteropServices;

namespace CardMaker.Desktop.Services;

public static class DesktopPathResolver
{
    public static string GetDataDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "CardMaker");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", "CardMaker");
        }

        // Linux and other Unix systems
        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (!string.IsNullOrWhiteSpace(xdgDataHome))
        {
            return Path.Combine(xdgDataHome, "CardMaker");
        }

        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userHome, ".local", "share", "CardMaker");
    }

    public static string GetDatabasePath(string? dataDir = null) =>
        Path.Combine(dataDir ?? GetDataDirectory(), "CardMaker.db");

    public static string GetAssetsDirectory(string? dataDir = null) =>
        Path.Combine(dataDir ?? GetDataDirectory(), "assets");

    public static string GetFontsDirectory(string? dataDir = null) =>
        Path.Combine(dataDir ?? GetDataDirectory(), "fonts");

    public static void EnsureDirectoriesCreated(string? dataDir = null)
    {
        var root = dataDir ?? GetDataDirectory();
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(GetAssetsDirectory(root));
        Directory.CreateDirectory(GetFontsDirectory(root));
    }
}

