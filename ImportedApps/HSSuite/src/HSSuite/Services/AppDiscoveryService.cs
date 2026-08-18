using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HSSuite.Models;

namespace HSSuite.Services
{
    internal static class AppDiscoveryService
    {
        private static readonly Regex VersionedSuiteName =
            new Regex(@"^HSSuite-v\d+\.\d+\.\d+\.exe$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static IReadOnlyList<SuiteApp> Discover()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            var currentPath = Path.GetFullPath(Process.GetCurrentProcess().MainModule.FileName);

            return Directory.GetFiles(directory, "HS*.exe", SearchOption.TopDirectoryOnly)
                .Where(path => !string.Equals(Path.GetFullPath(path), currentPath, StringComparison.OrdinalIgnoreCase))
                .Where(path => !IsSuiteLauncher(Path.GetFileName(path)))
                .Select(CreateApp)
                .OrderBy(app => app.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(app => app.FileName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static bool IsSuiteLauncher(string fileName)
        {
            return string.Equals(fileName, "HSSuite.exe", StringComparison.OrdinalIgnoreCase)
                || VersionedSuiteName.IsMatch(fileName);
        }

        private static SuiteApp CreateApp(string path)
        {
            var info = FileVersionInfo.GetVersionInfo(path);
            var fallbackName = Path.GetFileNameWithoutExtension(path);
            var displayName = string.IsNullOrWhiteSpace(info.ProductName) ? fallbackName : info.ProductName.Trim();
            var description = string.IsNullOrWhiteSpace(info.FileDescription)
                ? Path.GetFileName(path)
                : info.FileDescription.Trim();

            return new SuiteApp
            {
                DisplayName = displayName,
                Description = description,
                Version = NormalizeVersion(info.FileVersion),
                FileName = Path.GetFileName(path),
                FullPath = path
            };
        }

        private static string NormalizeVersion(string raw)
        {
            Version version;
            if (!Version.TryParse(raw, out version))
            {
                return string.Empty;
            }

            return string.Format("v{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }
    }
}
