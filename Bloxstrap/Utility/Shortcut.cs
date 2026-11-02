using System.Runtime.InteropServices;
using System.Windows;
using Bloxstrap.Resources;

namespace Bloxstrap.Utility
{
    internal static class Shortcut
    {
        private static GenericTriState _loadStatus = GenericTriState.Unknown;

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_FLUSH = 0x1000;

        public static void Create(string exePath, string exeArgs, string lnkPath, string? description = null)
        {
            const string LOG_IDENT = "Shortcut::Create";

            try
            {
                string? directory = Path.GetDirectoryName(lnkPath);
                if (!String.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType is null)
                    throw new InvalidOperationException("WScript.Shell is unavailable");

                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(lnkPath);
                shortcut.TargetPath = exePath;
                shortcut.Arguments = exeArgs ?? "";
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath) ?? "";
                shortcut.IconLocation = $"{exePath},0";
                shortcut.WindowStyle = 1;
                shortcut.Description = description ?? Path.GetFileNameWithoutExtension(lnkPath);
                shortcut.Save();

                NotifyShell();

                if (_loadStatus != GenericTriState.Successful)
                    _loadStatus = GenericTriState.Successful;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to create a shortcut for {lnkPath}!");
                App.Logger.WriteException(LOG_IDENT, ex);

                if (_loadStatus == GenericTriState.Failed)
                    return;

                _loadStatus = GenericTriState.Failed;

                Frontend.ShowMessageBox(Strings.Dialog_CannotCreateShortcuts, MessageBoxImage.Warning);
            }
        }

        public static void Delete(string lnkPath)
        {
            try
            {
                if (File.Exists(lnkPath))
                    File.Delete(lnkPath);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("Shortcut::Delete", ex);
            }
        }

        public static bool PointsToApplication(string lnkPath)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType is null)
                    return false;

                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(lnkPath);
                string target = (string)(shortcut.TargetPath ?? "");
                return String.Equals(target, Paths.Application, StringComparison.OrdinalIgnoreCase)
                    || String.Equals(target, Paths.Process, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static void NotifyShell()
        {
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
