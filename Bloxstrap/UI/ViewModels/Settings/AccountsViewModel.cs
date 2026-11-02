using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Models;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class AccountsViewModel : NotifyPropertyChangedViewModel
    {
        public ObservableCollection<Account> Accounts => AccountManager.Accounts;

        public string NewCookie { get; set; } = "";

        public string NewAlias { get; set; } = "";

        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); OnPropertyChanged(nameof(NotBusy)); OnPropertyChanged(nameof(CanLaunchSelected)); }
        }

        public bool NotBusy => !IsBusy;

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        private Account? _selectedAccount;
        public Account? SelectedAccount
        {
            get => _selectedAccount;
            set { _selectedAccount = value; OnPropertyChanged(nameof(SelectedAccount)); OnPropertyChanged(nameof(IsAccountSelected)); OnPropertyChanged(nameof(CanLaunchSelected)); }
        }

        public bool IsAccountSelected => SelectedAccount is not null;

        public string ActiveAccountAlias
        {
            get => String.IsNullOrEmpty(App.Settings.Prop.ActiveAccountAlias) ? "(none)" : App.Settings.Prop.ActiveAccountAlias;
        }

        public string LaunchPlaceInput
        {
            get => App.Settings.Prop.LastPlaceId;
            set
            {
                App.Settings.Prop.LastPlaceId = value;
                OnPropertyChanged(nameof(LaunchPlaceInput));
            }
        }

        public bool CanLaunchSelected => IsAccountSelected && NotBusy;

        public ICommand AddAccountCommand => new AsyncRelayCommand(AddAccountAsync);
        public ICommand RemoveAccountCommand => new RelayCommand(RemoveAccount);
        public ICommand SetActiveCommand => new RelayCommand(SetActive);
        public ICommand ClearActiveCommand => new RelayCommand(ClearActive);
        public ICommand RefreshCommand => new AsyncRelayCommand(RefreshSelectedAsync);
        public ICommand CopyTicketCommand => new AsyncRelayCommand(CopyTicketAsync);
        public ICommand LaunchSelectedAccountCommand => new AsyncRelayCommand(LaunchSelectedAccountAsync);
        public ICommand LaunchAllAccountsCommand => new AsyncRelayCommand(LaunchAllAccountsAsync);

        private async Task AddAccountAsync()
        {
            if (IsBusy)
                return;

            if (String.IsNullOrWhiteSpace(NewCookie))
            {
                StatusMessage = "Paste a .ROBLOSECURITY cookie first.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Validating cookie with Roblox...";

            var account = await AccountManager.AddOrUpdateAccountAsync(NewCookie, NewAlias);

            if (account is null)
            {
                StatusMessage = "That cookie is invalid or expired.";
            }
            else
            {
                StatusMessage = $"Added account '{account.Alias}' ({account.Username}).";
                NewCookie = "";
                NewAlias = "";
                OnPropertyChanged(nameof(NewCookie));
                OnPropertyChanged(nameof(NewAlias));
            }

            IsBusy = false;
        }

        private void RemoveAccount()
        {
            if (SelectedAccount is null)
                return;

            AccountManager.RemoveAccount(SelectedAccount);
            SelectedAccount = null;
            OnPropertyChanged(nameof(ActiveAccountAlias));
            StatusMessage = "Account removed.";
        }

        private void SetActive()
        {
            if (SelectedAccount is null)
                return;

            App.Settings.Prop.ActiveAccountAlias = SelectedAccount.Alias;
            App.Settings.Save();
            OnPropertyChanged(nameof(ActiveAccountAlias));
            StatusMessage = $"'{SelectedAccount.Alias}' will be used for the next game launch.";
        }

        private void ClearActive()
        {
            App.Settings.Prop.ActiveAccountAlias = "";
            App.Settings.Save();
            OnPropertyChanged(nameof(ActiveAccountAlias));
            StatusMessage = "Launches will use the default (browser) account.";
        }

        private async Task RefreshSelectedAsync()
        {
            if (SelectedAccount is null || IsBusy)
                return;

            IsBusy = true;
            StatusMessage = "Refreshing account details...";

            var account = await AccountManager.AddOrUpdateAccountAsync(SelectedAccount.Cookie, SelectedAccount.Alias);

            StatusMessage = account is null
                ? "Cookie is no longer valid - you may need to re-add this account."
                : $"Refreshed '{account.Alias}' ({account.Username}).";

            IsBusy = false;
        }

        private async Task CopyTicketAsync()
        {
            if (SelectedAccount is null || IsBusy)
                return;

            IsBusy = true;
            StatusMessage = "Generating authentication ticket...";

            string? ticket = await AccountManager.GenerateAuthTicketAsync(SelectedAccount.Cookie);

            if (String.IsNullOrEmpty(ticket))
            {
                StatusMessage = "Failed to generate a ticket.";
            }
            else
            {
                try
                {
                    Clipboard.SetText(ticket);
                    StatusMessage = "Authentication ticket copied to clipboard.";
                }
                catch
                {
                    StatusMessage = "Generated a ticket but couldn't access the clipboard.";
                }
            }

            IsBusy = false;
        }

        private async Task LaunchSelectedAccountAsync()
        {
            if (SelectedAccount is null || IsBusy)
                return;

            long? placeId = RobloxLauncher.ParsePlaceId(LaunchPlaceInput);

            if (placeId is null)
            {
                StatusMessage = "Enter a place ID or game URL first.";
                return;
            }

            IsBusy = true;
            StatusMessage = $"Launching '{SelectedAccount.Alias}'…";

            try
            {
                int started = await RobloxLauncher.LaunchCopiesAsync(placeId.Value, SelectedAccount, 1);
                StatusMessage = started == 0 ? "Could not start Roblox." : $"Started Roblox as '{SelectedAccount.Alias}'.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Launch failed: " + ex.Message;
            }

            IsBusy = false;
        }

        private async Task LaunchAllAccountsAsync()
        {
            if (IsBusy)
                return;

            long? placeId = RobloxLauncher.ParsePlaceId(LaunchPlaceInput);

            if (placeId is null)
            {
                StatusMessage = "Enter a place ID or game URL first.";
                return;
            }

            if (Accounts.Count == 0)
            {
                StatusMessage = "Add at least one account first.";
                return;
            }

            IsBusy = true;
            StatusMessage = $"Launching {Accounts.Count} account(s)…";

            try
            {
                int started = await RobloxLauncher.LaunchAccountsAsync(placeId.Value, Accounts.ToList());
                StatusMessage = started == 0 ? "Could not start Roblox." : $"Started {started} Roblox window(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = "Launch failed: " + ex.Message;
            }

            IsBusy = false;
        }
    }
}
