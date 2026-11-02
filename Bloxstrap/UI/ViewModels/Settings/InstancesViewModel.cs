using System.Collections.ObjectModel;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class InstancesViewModel : NotifyPropertyChangedViewModel
    {
        public ObservableCollection<RobloxInstanceInfo> Instances { get; } = new();

        private RobloxInstanceInfo? _selectedInstance;
        public RobloxInstanceInfo? SelectedInstance
        {
            get => _selectedInstance;
            set
            {
                _selectedInstance = value;
                OnPropertyChanged(nameof(SelectedInstance));
                OnPropertyChanged(nameof(IsInstanceSelected));
            }
        }

        public bool IsInstanceSelected => SelectedInstance is not null;

        public string InstanceCountLabel => Instances.Count == 1
            ? "1 Roblox window is running."
            : $"{Instances.Count} Roblox windows are running.";

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
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
                    MultiInstance.EnsureMutexWatcherRunning();
                }
                else
                {
                    MultiInstance.StopMutexWatcher();
                }
            }
        }

        public ICommand RefreshCommand => new RelayCommand(Refresh);
        public ICommand FocusCommand => new RelayCommand(FocusSelected);
        public ICommand CloseSelectedCommand => new RelayCommand(CloseSelected);
        public ICommand CloseAllCommand => new RelayCommand(CloseAll);
        public ICommand TileGridCommand => new RelayCommand(() => Tile(WindowTileMode.Grid));
        public ICommand TileHorizontalCommand => new RelayCommand(() => Tile(WindowTileMode.Horizontal));
        public ICommand TileVerticalCommand => new RelayCommand(() => Tile(WindowTileMode.Vertical));

        public InstancesViewModel()
        {
            Refresh();
        }

        public void Refresh()
        {
            var currentPid = SelectedInstance?.ProcessId;
            Instances.Clear();

            foreach (var instance in MultiInstance.GetRunningInstances())
                Instances.Add(instance);

            if (currentPid is not null)
                SelectedInstance = Instances.FirstOrDefault(x => x.ProcessId == currentPid);

            OnPropertyChanged(nameof(InstanceCountLabel));
            StatusMessage = Instances.Count == 0
                ? "No Roblox clients are running."
                : $"Updated {DateTime.Now:t}.";
        }

        private void FocusSelected()
        {
            if (SelectedInstance is null)
                return;

            if (!MultiInstance.FocusInstance(SelectedInstance))
                StatusMessage = "Could not bring that window to the front.";
        }

        private void CloseSelected()
        {
            if (SelectedInstance is null)
                return;

            int closed = MultiInstance.CloseInstance(SelectedInstance.ProcessId);
            StatusMessage = closed == 0 ? "Could not close that instance." : "Closed the selected instance.";
            Refresh();
        }

        private void CloseAll()
        {
            int closed = MultiInstance.CloseAllRobloxInstances();
            StatusMessage = $"Closed {closed} Roblox instance(s).";
            Refresh();
        }

        private void Tile(WindowTileMode mode)
        {
            Refresh();
            int tiled = WindowLayout.Tile(Instances.ToList(), mode);
            StatusMessage = tiled == 0 ? "No Roblox windows to tile." : $"Tiled {tiled} window(s).";
        }
    }
}
