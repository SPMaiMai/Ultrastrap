namespace Bloxstrap.Models
{
    /// <summary>
    /// A running Roblox client process surfaced in the instance dashboard.
    /// </summary>
    public class RobloxInstanceInfo
    {
        public int ProcessId { get; set; }

        public string ProcessName { get; set; } = "";

        public string WindowTitle { get; set; } = "";

        public DateTime StartTime { get; set; }

        public long WorkingSetBytes { get; set; }

        public nint MainWindowHandle { get; set; }

        public string MemoryLabel => $"{WorkingSetBytes / (1024 * 1024)} MB";

        public string StartedLabel
        {
            get
            {
                try
                {
                    return StartTime.ToString("t");
                }
                catch
                {
                    return "";
                }
            }
        }

        public string DisplayLabel
        {
            get
            {
                string title = String.IsNullOrWhiteSpace(WindowTitle) ? ProcessName : WindowTitle;
                return $"{title}  ·  PID {ProcessId}  ·  {MemoryLabel}";
            }
        }
    }
}
