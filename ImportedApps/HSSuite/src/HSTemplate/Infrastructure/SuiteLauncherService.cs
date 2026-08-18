using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace HSTemplate.Infrastructure
{
    internal static class SuiteLauncherService
    {
        public static bool IsAvailable
        {
            get { return FindSuiteExecutable() != null; }
        }

        public static bool TryOpen(out string error)
        {
            error = null;
            var path = FindSuiteExecutable();
            if (path == null)
            {
                error = "HSSuite wurde in diesem Ordner nicht gefunden.";
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo(path)
                {
                    WorkingDirectory = Path.GetDirectoryName(path),
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                error = "HSSuite konnte nicht geöffnet werden: " + ex.Message;
                return false;
            }
        }

        private static string FindSuiteExecutable()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            var exactPath = Path.Combine(directory, "HSSuite.exe");
            if (File.Exists(exactPath))
            {
                return exactPath;
            }

            try
            {
                return Directory.GetFiles(directory, "HSSuite-v*.exe", SearchOption.TopDirectoryOnly)
                    .Select(path => new
                    {
                        Path = path,
                        Version = TryGetVersion(Path.GetFileNameWithoutExtension(path))
                    })
                    .OrderByDescending(item => item.Version)
                    .ThenByDescending(item => item.Path, StringComparer.OrdinalIgnoreCase)
                    .Select(item => item.Path)
                    .FirstOrDefault();
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static Version TryGetVersion(string fileNameWithoutExtension)
        {
            const string prefix = "HSSuite-v";
            if (!fileNameWithoutExtension.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return new Version(0, 0, 0);
            }

            Version version;
            return Version.TryParse(fileNameWithoutExtension.Substring(prefix.Length), out version)
                ? version
                : new Version(0, 0, 0);
        }
    }
}
