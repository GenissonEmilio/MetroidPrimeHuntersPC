using System;
using System.IO;

namespace MphRead
{
    public static class PcStorage
    {
        public static string DataDirectory => GetDataDirectory(
            OperatingSystem.IsLinux(), AppContext.BaseDirectory,
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetEnvironmentVariable("XDG_DATA_HOME"),
            Environment.GetEnvironmentVariable("HUNTERSPC_DATA_DIR"));

        public static string GetDataDirectory(bool linux, string application, string home, string? xdg, string? overridePath)
        {
            if (!String.IsNullOrWhiteSpace(overridePath)) return Path.GetFullPath(overridePath);
            if (!linux) return application;
            // The XDG specification requires an absolute base directory.
            string root = !String.IsNullOrWhiteSpace(xdg) && Path.IsPathFullyQualified(xdg)
                ? xdg : Path.Combine(home, ".local", "share");
            return Path.Combine(root, "hunterspc");
        }
    }
}
