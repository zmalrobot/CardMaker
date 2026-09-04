using System.Runtime.InteropServices;

namespace CardMaker.Desktop;

/// <summary>
/// Configurazione dell'identità nativa del processo CardMaker a livello di sistema operativo.
/// Imposta prgname/application_name su Linux (GTK/GLib) affinché WM_CLASS e Task Switcher associno
/// la finestra al desktop launcher CardMaker, e AppUserModelID su Windows per il raggruppamento Taskbar/Start Menu.
/// Su Linux, provvede inoltre alla registrazione automatica degli asset FreeDesktop (desktop file e icone hicolor).
/// </summary>
internal static class PlatformBranding
{
    public const string AppName = "CardMaker";

    public static void Initialize()
    {
        if (OperatingSystem.IsLinux())
        {
            try
            {
                LinuxInterop.SetNames(AppName);
                EnsureLinuxDesktopAssets();
            }
            catch
            {
                // Fallback silenzioso in caso di ambienti Linux minimali privi di GLib/GTK
            }
        }
        else if (OperatingSystem.IsWindows())
        {
            try
            {
                WindowsInterop.SetAppUserModelId(AppName);
            }
            catch
            {
                // Fallback silenzioso se eseguito su versioni Windows embedded/legacy
            }
        }
    }

    private static void EnsureLinuxDesktopAssets()
    {
        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(home) || !Directory.Exists(home))
            {
                return;
            }

            var appsDir = Path.Combine(home, ".local", "share", "applications");
            var iconsBaseDir = Path.Combine(home, ".local", "share", "icons", "hicolor");

            Directory.CreateDirectory(appsDir);
            Directory.CreateDirectory(iconsBaseDir);

            // Locate source desktop file & icons in base directory or repo
            var baseDir = AppContext.BaseDirectory;
            var resourcesDir = Path.Combine(baseDir, "Resources");
            if (!Directory.Exists(resourcesDir))
            {
                var dir = new DirectoryInfo(baseDir);
                while (dir != null && dir.Exists)
                {
                    var candidate = Path.Combine(dir.FullName, "src", "CardMaker.Desktop", "Resources");
                    if (Directory.Exists(candidate))
                    {
                        resourcesDir = candidate;
                        break;
                    }
                    dir = dir.Parent;
                }
            }

            if (Directory.Exists(resourcesDir))
            {
                var desktopSrc = Path.Combine(resourcesDir, "cardmaker.desktop");
                if (File.Exists(desktopSrc))
                {
                    File.Copy(desktopSrc, Path.Combine(appsDir, "cardmaker.desktop"), true);
                    File.Copy(desktopSrc, Path.Combine(appsDir, "CardMaker.desktop"), true);
                }

                var iconsSrc = Path.Combine(resourcesDir, "icons", "hicolor");
                if (Directory.Exists(iconsSrc))
                {
                    foreach (var size in new[] { "16x16", "32x32", "48x48", "64x64", "128x128", "256x256", "512x512" })
                    {
                        var srcIcon = Path.Combine(iconsSrc, size, "apps", "cardmaker.png");
                        if (File.Exists(srcIcon))
                        {
                            var targetDir = Path.Combine(iconsBaseDir, size, "apps");
                            Directory.CreateDirectory(targetDir);
                            File.Copy(srcIcon, Path.Combine(targetDir, "cardmaker.png"), true);
                            File.Copy(srcIcon, Path.Combine(targetDir, "CardMaker.png"), true);
                        }
                    }
                }
            }

            // Create symlink or binary copy in ~/.local/bin/CardMaker if directory exists
            var localBin = Path.Combine(home, ".local", "bin");
            if (Directory.Exists(localBin))
            {
                var targetExe = Path.Combine(localBin, "CardMaker");
                var currentExe = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(currentExe) && File.Exists(currentExe) && currentExe != targetExe)
                {
                    try
                    {
                        if (File.Exists(targetExe))
                        {
                            File.Delete(targetExe);
                        }
                        File.CreateSymbolicLink(targetExe, currentExe);
                    }
                    catch { }
                }
            }
        }
        catch
        {
            // Silently ignore if in restricted sandbox
        }
    }

    private static class LinuxInterop
    {
        private const int PR_SET_NAME = 15;

        [DllImport("libglib-2.0.so.0", EntryPoint = "g_set_prgname", BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern void GSetPrgname([MarshalAs(UnmanagedType.LPStr)] string prgname);

        [DllImport("libglib-2.0.so.0", EntryPoint = "g_set_application_name", BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern void GSetApplicationName([MarshalAs(UnmanagedType.LPStr)] string applicationName);

        [DllImport("libc", EntryPoint = "prctl", BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern int Prctl(int option, [MarshalAs(UnmanagedType.LPStr)] string arg2, ulong arg3, ulong arg4, ulong arg5);

        [DllImport("libgtk-3.so.0", EntryPoint = "gtk_init_check")]
        private static extern int GtkInitCheck(IntPtr argc, IntPtr argv);

        [DllImport("libgtk-3.so.0", EntryPoint = "gtk_window_set_default_icon_from_file", BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern int GtkWindowSetDefaultIconFromFile([MarshalAs(UnmanagedType.LPStr)] string filename, IntPtr err);

        [DllImport("libgtk-3.so.0", EntryPoint = "gtk_window_set_default_icon_name", BestFitMapping = false, ThrowOnUnmappableChar = true)]
        private static extern void GtkWindowSetDefaultIconName([MarshalAs(UnmanagedType.LPStr)] string name);

        public static void SetNames(string name)
        {
            try
            {
                GSetPrgname(name);
            }
            catch { }

            try
            {
                GSetApplicationName(name);
            }
            catch { }

            try
            {
                int res = Prctl(PR_SET_NAME, name, 0, 0, 0);
                if (res != 0)
                {
                    // Fallback silenzioso
                }
            }
            catch { }

            try
            {
                int init = GtkInitCheck(IntPtr.Zero, IntPtr.Zero);
                if (init != 0)
                {
                    GtkWindowSetDefaultIconName("cardmaker");
                    GtkWindowSetDefaultIconName(name);

                    // Locate icon.png
                    var iconPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "icon.png");
                    if (!File.Exists(iconPath))
                    {
                        var dir = new DirectoryInfo(AppContext.BaseDirectory);
                        while (dir != null && dir.Exists)
                        {
                            var candidate = Path.Combine(dir.FullName, "src", "CardMaker.Desktop", "wwwroot", "icon.png");
                            if (File.Exists(candidate))
                            {
                                iconPath = candidate;
                                break;
                            }
                            dir = dir.Parent;
                        }
                    }

                    if (File.Exists(iconPath))
                    {
                        int iconRes = GtkWindowSetDefaultIconFromFile(iconPath, IntPtr.Zero);
                        if (iconRes == 0)
                        {
                            // Icon failed to load
                        }
                    }
                }
            }
            catch { }
        }
    }

    private static class WindowsInterop
    {
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appID);

        public static void SetAppUserModelId(string id)
        {
            int hr = SetCurrentProcessExplicitAppUserModelID(id);
            if (hr < 0)
            {
                // Fallback silenzioso
            }
        }
    }
}
