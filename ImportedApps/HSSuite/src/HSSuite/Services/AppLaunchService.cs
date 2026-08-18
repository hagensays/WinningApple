using System;
using System.Diagnostics;
using System.IO;

namespace HSSuite.Services
{
    internal static class AppLaunchService
    {
        public static bool TryLaunch(string path, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                error = "Die Anwendung wurde nicht gefunden.";
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
                error = "Die Anwendung konnte nicht geöffnet werden: " + ex.Message;
                return false;
            }
        }
    }
}
