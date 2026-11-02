using System.Collections.ObjectModel;

namespace Bloxstrap.Models.Persistable
{
    public class Settings
    {
        // bloxstrap configuration
        public BootstrapperStyle BootstrapperStyle { get; set; } = BootstrapperStyle.FluentDialog;
        public BootstrapperIcon BootstrapperIcon { get; set; } = BootstrapperIcon.IconBloxstrap;
        public string BootstrapperTitle { get; set; } = App.ProjectName;
        public string BootstrapperIconCustomLocation { get; set; } = "";
        public Theme Theme { get; set; } = Theme.Default;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool DeveloperMode { get; set; } = false;
        public bool CheckForUpdates { get; set; } = true;
        public bool ConfirmLaunches { get; set; } = false;
        public string Locale { get; set; } = "nil";
        public bool UseFastFlagManager { get; set; } = true;
        public bool WPFSoftwareRender { get; set; } = false;
        public bool EnableAnalytics { get; set; } = true;
        public bool BackgroundUpdatesEnabled { get; set; } = false;
        public bool DebugDisableVersionPackageCleanup { get; set; } = false;
        public string? SelectedCustomTheme { get; set; } = null;
        public WebEnvironment WebEnvironment { get; set; } = WebEnvironment.Production;

        // integration configuration
        public bool EnableActivityTracking { get; set; } = true;
        public bool UseDiscordRichPresence { get; set; } = true;
        public bool HideRPCButtons { get; set; } = true;
        public bool ShowAccountOnRichPresence { get; set; } = false;
        public bool ShowServerDetails { get; set; } = false;
        public ObservableCollection<CustomIntegration> CustomIntegrations { get; set; } = new();

        // mod preset configuration
        public bool UseDisableAppPatch { get; set; } = false;

        // ultrastrap: multi-instance configuration
        /// <summary>
        /// Allows multiple Roblox windows (same or different accounts) to run simultaneously by
        /// holding the Roblox singleton mutexes open via a background watcher.
        /// </summary>
        public bool MultiInstanceLaunching { get; set; } = false;

        /// <summary>
        /// Keep the mutex watcher running even after the last Roblox client closes, so the next
        /// multi-window launch does not have to wait for the watcher to spawn again.
        /// </summary>
        public bool PersistMutexWatcher { get; set; } = false;

        /// <summary>
        /// After a multi-window launch, automatically tile the new Roblox windows on the desktop.
        /// </summary>
        public bool AutoTileWindows { get; set; } = false;

        /// <summary>
        /// After a multi-window launch, rename each Roblox window to include the account alias.
        /// </summary>
        public bool RenameWindowsByAccount { get; set; } = true;

        /// <summary>
        /// Delay between successive Roblox process starts, in milliseconds. Tickets are single-use,
        /// so launches have to be staggered.
        /// </summary>
        public int LaunchStaggerMs { get; set; } = 1500;

        // ultrastrap: account manager configuration
        /// <summary>
        /// The alias of the account whose authentication ticket should be injected into player launches.
        /// Empty means "use whatever the browser/launch URI already provides".
        /// </summary>
        public string ActiveAccountAlias { get; set; } = "";

        /// <summary>
        /// Last place ID (or URL) typed into the Quick Launch page.
        /// </summary>
        public string LastPlaceId { get; set; } = "";
    }
}
