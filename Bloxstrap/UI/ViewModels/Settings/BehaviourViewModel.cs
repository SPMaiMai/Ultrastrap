namespace Bloxstrap.UI.ViewModels.Settings
{
    public class BehaviourViewModel : NotifyPropertyChangedViewModel
    {
        public bool ConfirmLaunches
        {
            get => App.Settings.Prop.ConfirmLaunches;
            set => App.Settings.Prop.ConfirmLaunches = value;
        }

        public bool BackgroundUpdates
        {
            get => App.Settings.Prop.BackgroundUpdatesEnabled;
            set => App.Settings.Prop.BackgroundUpdatesEnabled = value;
        }

        public bool MultiInstanceLaunching
        {
            get => App.Settings.Prop.MultiInstanceLaunching;
            set
            {
                App.Settings.Prop.MultiInstanceLaunching = value;
                if (value)
                    MultiInstance.EnsureMutexWatcherRunning();
                else if (!App.Settings.Prop.PersistMutexWatcher)
                    MultiInstance.StopMutexWatcher();
            }
        }

        public bool PersistMutexWatcher
        {
            get => App.Settings.Prop.PersistMutexWatcher;
            set
            {
                App.Settings.Prop.PersistMutexWatcher = value;
                if (value)
                {
                    App.Settings.Prop.MultiInstanceLaunching = true;
                    OnPropertyChanged(nameof(MultiInstanceLaunching));
                    MultiInstance.EnsureMutexWatcherRunning();
                }
            }
        }

        public bool AutoTileWindows
        {
            get => App.Settings.Prop.AutoTileWindows;
            set => App.Settings.Prop.AutoTileWindows = value;
        }

        public bool RenameWindowsByAccount
        {
            get => App.Settings.Prop.RenameWindowsByAccount;
            set => App.Settings.Prop.RenameWindowsByAccount = value;
        }

        public double LaunchStaggerMs
        {
            get => App.Settings.Prop.LaunchStaggerMs;
            set => App.Settings.Prop.LaunchStaggerMs = (int)Math.Clamp(value, 250, 10_000);
        }

        public bool IsRobloxInstallationMissing => !App.IsPlayerInstalled && !App.IsStudioInstalled;

        public bool ForceRobloxReinstallation
        {
            get => App.State.Prop.ForceReinstall || IsRobloxInstallationMissing;
            set => App.State.Prop.ForceReinstall = value;
        }
    }
}
