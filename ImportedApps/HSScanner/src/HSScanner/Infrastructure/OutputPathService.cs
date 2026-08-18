using System;
using System.IO;

namespace HSScanner.Infrastructure
{
    public static class OutputPathService
    {
        public static string ApplicationDirectory
        {
            get
            {
                return Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
            }
        }

        public static string GetUniqueOutputPath(string baseName, string extension)
        {
            if (string.IsNullOrWhiteSpace(baseName)) throw new ArgumentException("baseName is required", "baseName");
            if (string.IsNullOrWhiteSpace(extension)) throw new ArgumentException("extension is required", "extension");

            var safeBaseName = SanitizeFileName(baseName);
            var normalizedExtension = extension.StartsWith(".", StringComparison.Ordinal) ? extension : "." + extension;
            var directory = ApplicationDirectory;
            var candidate = Path.Combine(directory, safeBaseName + normalizedExtension);
            var index = 2;

            while (File.Exists(candidate) || Directory.Exists(candidate))
            {
                candidate = Path.Combine(directory, safeBaseName + "_" + index + normalizedExtension);
                index++;
            }

            return candidate;
        }

        private static string SanitizeFileName(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }
            return value.Trim();
        }
    }
}
