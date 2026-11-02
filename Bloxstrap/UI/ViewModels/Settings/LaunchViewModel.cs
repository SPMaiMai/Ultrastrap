using System.Collections.ObjectModel;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class LaunchViewModel : NotifyPropertyChangedViewModel
    {
        public ObservableCollection<Account> Accounts => AccountManager.Accounts;

        public ObservableCollection<FavoritePlace> Favorites => App.Favorites.Prop.Places;

        public string PlaceInput
        {
            get => App.Settings.Prop.LastPlaceId;
            set
            {
                App.Settings.Prop.LastPlaceId = value;
                OnPropertyChanged(nameof(PlaceInput));
            }
        }

        public string JobId { get; set; } = "";

        public string AccessCode { get; set; } = "";

        public string NewFavoriteName { get; set; } = "";

        public double Copies { get; set; } = 1;

        public double CopiesMinimum => 1;

        public double CopiesMaximum => 16;

        private Account? _selectedAccount;
        public Account? SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                _selectedAccount = value;
                OnPropertyChanged(nameof(SelectedAccount));
                OnPropertyChanged(nameof(IsAccountSelected));
            }
        }

        public bool IsAccountSelected => SelectedAccount is not null;

        private FavoritePlace? _selectedFavorite;
        public FavoritePlace? SelectedFavorite
        {
            get => _selectedFavorite;
            set
            {
                _selectedFavorite = value;
                OnPropertyChanged(nameof(SelectedFavorite));
                OnPropertyChanged(nameof(IsFavoriteSelected));

                if (value is not null)
                {
                    PlaceInput = value.PlaceId.ToString();
                    JobId = value.JobId ?? "";
                    AccessCode = value.AccessCode ?? "";
                    OnPropertyChanged(nameof(JobId));
                    OnPropertyChanged(nameof(AccessCode));
                }
            }
        }

        public bool IsFavoriteSelected => SelectedFavorite is not null;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(NotBusy));
            }
        }

        public bool NotBusy => !IsBusy;

        private string _statusMessage = "Enter a place ID or Roblox game URL, then launch one or more windows.";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        public bool MultiInstanceLaunching
        {
            get => App.Settings.Prop.MultiInstanceLaunching;
            set
            {
                App.Settings.Prop.MultiInstanceLaunching = value;
                if (value)
                    MultiInstance.EnsureMutexWatcherRunning();
                OnPropertyChanged(nameof(MultiInstanceLaunching));
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

        public ICommand LaunchSelectedCommand => new AsyncRelayCommand(LaunchSelectedAsync);
        public ICommand LaunchCopiesCommand => new AsyncRelayCommand(LaunchCopiesAsync);
        public ICommand LaunchAllAccountsCommand => new AsyncRelayCommand(LaunchAllAccountsAsync);
        public ICommand AddFavoriteCommand => new RelayCommand(AddFavorite);
        public ICommand RemoveFavoriteCommand => new RelayCommand(RemoveFavorite);
        public ICommand UseActiveAccountCommand => new RelayCommand(UseActiveAccount);

        public LaunchViewModel()
        {
            string active = App.Settings.Prop.ActiveAccountAlias;
            if (!String.IsNullOrEmpty(active))
                SelectedAccount = AccountManager.GetAccount(active);
        }

        private bool TryGetPlace(out long placeId)
        {
            long? parsed = RobloxLauncher.ParsePlaceId(PlaceInput);

            if (parsed is null)
            {
                StatusMessage = "Enter a numeric place ID or a roblox.com/games/... URL.";
                placeId = 0;
                return false;
            }

            placeId = parsed.Value;
            App.Settings.Prop.LastPlaceId = PlaceInput;
            App.Settings.Save();
            return true;
        }

        private string? OptionalJobId => String.IsNullOrWhiteSpace(JobId) ? null : JobId.Trim();

        private string? OptionalAccessCode => String.IsNullOrWhiteSpace(AccessCode) ? null : AccessCode.Trim();

        private Progress<string> StatusProgress => new(message => StatusMessage = message);

        private async Task LaunchSelectedAsync()
        {
            if (IsBusy || !TryGetPlace(out long placeId))
                return;

            IsBusy = true;

            try
            {
                await RobloxLauncher.LaunchCopiesAsync(placeId, SelectedAccount, 1, OptionalJobId, OptionalAccessCode, StatusProgress);
            }
            catch (Exception ex)
            {
                StatusMessage = "Launch failed: " + ex.Message;
            }

            IsBusy = false;
        }

        private async Task LaunchCopiesAsync()
        {
            if (IsBusy || !TryGetPlace(out long placeId))
                return;

            IsBusy = true;

            try
            {
                await RobloxLauncher.LaunchCopiesAsync(placeId, SelectedAccount, (int)Copies, OptionalJobId, OptionalAccessCode, StatusProgress);
            }
            catch (Exception ex)
            {
                StatusMessage = "Launch failed: " + ex.Message;
            }

            IsBusy = false;
        }

        private async Task LaunchAllAccountsAsync()
        {
            if (IsBusy || !TryGetPlace(out long placeId))
                return;

            if (Accounts.Count == 0)
            {
                StatusMessage = "Add at least one account on the Accounts page first.";
                return;
            }

            IsBusy = true;

            try
            {
                await RobloxLauncher.LaunchAccountsAsync(placeId, Accounts.ToList(), OptionalJobId, OptionalAccessCode, StatusProgress);
            }
            catch (Exception ex)
            {
                StatusMessage = "Launch failed: " + ex.Message;
            }

            IsBusy = false;
        }

        private void AddFavorite()
        {
            if (!TryGetPlace(out long placeId))
                return;

            string name = String.IsNullOrWhiteSpace(NewFavoriteName) ? $"Place {placeId}" : NewFavoriteName.Trim();

            var existing = Favorites.FirstOrDefault(x => x.PlaceId == placeId && (x.JobId ?? "") == (OptionalJobId ?? "") && (x.AccessCode ?? "") == (OptionalAccessCode ?? ""));

            if (existing is not null)
            {
                existing.Name = name;
                existing.LastUsed = DateTime.UtcNow;
                StatusMessage = $"Updated favorite '{name}'.";
            }
            else
            {
                Favorites.Add(new FavoritePlace
                {
                    Name = name,
                    PlaceId = placeId,
                    JobId = OptionalJobId,
                    AccessCode = OptionalAccessCode,
                    LastUsed = DateTime.UtcNow
                });
                StatusMessage = $"Saved favorite '{name}'.";
            }

            NewFavoriteName = "";
            OnPropertyChanged(nameof(NewFavoriteName));
            App.Favorites.Save();
        }

        private void RemoveFavorite()
        {
            if (SelectedFavorite is null)
                return;

            Favorites.Remove(SelectedFavorite);
            SelectedFavorite = null;
            App.Favorites.Save();
            StatusMessage = "Favorite removed.";
        }

        private void UseActiveAccount()
        {
            if (SelectedAccount is null)
                return;

            App.Settings.Prop.ActiveAccountAlias = SelectedAccount.Alias;
            App.Settings.Save();
            StatusMessage = $"'{SelectedAccount.Alias}' is now the default launch account.";
        }
    }
}
