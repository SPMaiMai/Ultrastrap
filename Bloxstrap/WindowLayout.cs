using System.Runtime.InteropServices;

namespace Bloxstrap
{
    public enum WindowTileMode
    {
        Grid,
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Positions and labels Roblox client windows. Used when several instances are open at once.
    /// </summary>
    public static class WindowLayout
    {
        private const int SW_RESTORE = 9;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(nint hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(nint hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SetWindowText(nint hWnd, string lpString);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(nint hWnd);

        public static bool BringToFront(nint handle)
        {
            if (handle == 0)
                return false;

            if (IsIconic(handle))
                ShowWindow(handle, SW_RESTORE);

            return SetForegroundWindow(handle);
        }

        public static bool SetTitle(nint handle, string title)
        {
            if (handle == 0 || String.IsNullOrEmpty(title))
                return false;

            return SetWindowText(handle, title);
        }

        public static int Tile(IReadOnlyList<RobloxInstanceInfo> instances, WindowTileMode mode = WindowTileMode.Grid)
        {
            var windows = instances
                .Where(x => x.MainWindowHandle != 0 && IsWindowVisible(x.MainWindowHandle))
                .Select(x => x.MainWindowHandle)
                .Distinct()
                .ToList();

            if (windows.Count == 0)
                return 0;

            var bounds = GetWorkingArea();
            int count = windows.Count;
            int cols;
            int rows;

            switch (mode)
            {
                case WindowTileMode.Horizontal:
                    cols = count;
                    rows = 1;
                    break;
                case WindowTileMode.Vertical:
                    cols = 1;
                    rows = count;
                    break;
                default:
                    cols = (int)Math.Ceiling(Math.Sqrt(count));
                    rows = (int)Math.Ceiling(count / (double)cols);
                    break;
            }

            int cellWidth = Math.Max(320, bounds.Width / cols);
            int cellHeight = Math.Max(240, bounds.Height / rows);
            int tiled = 0;

            for (int i = 0; i < windows.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                int x = bounds.X + (col * cellWidth);
                int y = bounds.Y + (row * cellHeight);

                ShowWindow(windows[i], SW_RESTORE);

                if (SetWindowPos(windows[i], 0, x, y, cellWidth, cellHeight, SWP_NOZORDER | SWP_SHOWWINDOW))
                    tiled++;
            }

            return tiled;
        }

        public static void ApplyAccountTitles(IReadOnlyList<RobloxInstanceInfo> instances, IReadOnlyList<string> labels)
        {
            if (App.Settings.Prop.RenameWindowsByAccount == false)
                return;

            int n = Math.Min(instances.Count, labels.Count);

            for (int i = 0; i < n; i++)
            {
                if (String.IsNullOrWhiteSpace(labels[i]) || instances[i].MainWindowHandle == 0)
                    continue;

                SetTitle(instances[i].MainWindowHandle, $"Roblox — {labels[i]}");
            }
        }

        private static (int X, int Y, int Width, int Height) GetWorkingArea()
        {
            var area = System.Windows.Forms.Screen.PrimaryScreen?.WorkingArea
                       ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);

            return (area.X, area.Y, area.Width, area.Height);
        }
    }
}
