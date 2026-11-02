using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using Windows.Win32;
using Windows.Win32.Foundation;

using Bloxstrap.UI.Elements.Dialogs;
using Bloxstrap.UI.Elements.Settings.Pages;
using Bloxstrap.Enums;

namespace Bloxstrap
{
    public static class LaunchHandler
    {
        public static void ProcessNextAction(NextAction action, bool isUnfinishedInstall = false)
        {
            const string LOG_IDENT = "LaunchHandler::ProcessNextAction";

            switch (action)
            {
                case NextAction.LaunchSettings:
                    App.Logger.WriteLine(LOG_IDENT, "Opening settings");
                    LaunchSettings();
                    break;

                case NextAction.LaunchRoblox:
                    App.Logger.WriteLine(LOG_IDENT, "Opening Roblox");
                    LaunchRoblox(LaunchMode.Player);
                    break;

                case NextAction.LaunchRobloxStudio:
                    App.Logger.WriteLine(LOG_IDENT, "Opening Roblox Studio");
                    LaunchRoblox(LaunchMode.Studio);
                    break;

                case NextAction.LaunchAccounts:
                    App.Logger.WriteLine(LOG_IDENT, "Opening account manager");
                    LaunchSettingsPage(typeof(UI.Elements.Settings.Pages.AccountsPage));
                    break;

                case NextAction.LaunchQuickLaunch:
                    App.Logger.WriteLine(LOG_IDENT, "Opening quick launch");
                    LaunchSettingsPage(typeof(UI.Elements.Settings.Pages.LaunchPage));
                    break;

                case NextAction.LaunchInstances:
                    App.Logger.WriteLine(LOG_IDENT, "Opening instance dashboard");
                    LaunchSettingsPage(typeof(UI.Elements.Settings.Pages.InstancesPage));
                    break;

                default:
                    App.Logger.WriteLine(LOG_IDENT, "Closing");
                    App.Terminate(isUnfinishedInstall ? ErrorCode.ERROR_INSTALL_USEREXIT : ErrorCode.ERROR_SUCCESS);
                    break;
            }
        }

        public static void ProcessLaunchArgs()
        {
            const string LOG_IDENT = "LaunchHandler::ProcessLaunchArgs";

            // this order is specific

            if (App.LaunchSettings.MultiInstanceWatcherFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Running multi-instance mutex watcher");
                MultiInstance.RunMutexWatcher();
                App.Terminate();
            }
            else if (App.LaunchSettings.UninstallFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening uninstaller");
                LaunchUninstaller();
            }
            else if (App.LaunchSettings.AccountsFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening account manager");
                LaunchSettingsPage(typeof(UI.Elements.Settings.Pages.AccountsPage));
            }
            else if (App.LaunchSettings.InstancesFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening instance dashboard");
                LaunchSettingsPage(typeof(UI.Elements.Settings.Pages.InstancesPage));
            }
            else if (App.LaunchSettings.LaunchPageFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening quick launch");
                LaunchSettingsPage(typeof(UI.Elements.Settings.Pages.LaunchPage));
            }
            else if (App.LaunchSettings.PlaceFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening Roblox via place flag");
                LaunchPlaceFromFlags();
            }
            else if (App.LaunchSettings.MenuFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening settings");
                LaunchSettings();
            }
            else if (App.LaunchSettings.WatcherFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening watcher");
                LaunchWatcher();
            }
            else if (App.LaunchSettings.BackgroundUpdaterFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening background updater");
                LaunchBackgroundUpdater();
            }
            else if (App.LaunchSettings.RobloxLaunchMode != LaunchMode.None)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Opening bootstrapper ({App.LaunchSettings.RobloxLaunchMode})");
                LaunchRoblox(App.LaunchSettings.RobloxLaunchMode);
            }
            else if (!App.LaunchSettings.QuietFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Opening menu");
                LaunchMenu();
            }
            else
            {
                App.Logger.WriteLine(LOG_IDENT, "Closing - quiet flag active");
                App.Terminate();
            }
        }

        public static void LaunchInstaller()
        {
            using var interlock = new InterProcessLock("Installer");

            if (!interlock.IsAcquired)
            {
                Frontend.ShowMessageBox(Strings.Dialog_AlreadyRunning_Installer, MessageBoxImage.Stop);
                App.Terminate();
                return;
            }

            if (App.LaunchSettings.UninstallFlag.Active)
            {
                Frontend.ShowMessageBox(Strings.Bootstrapper_FirstRunUninstall, MessageBoxImage.Error);
                App.Terminate(ErrorCode.ERROR_INVALID_FUNCTION);
                return;
            }

            if (App.LaunchSettings.QuietFlag.Active)
            {
                var installer = new Installer();

                if (!installer.CheckInstallLocation())
                    App.Terminate(ErrorCode.ERROR_INSTALL_FAILURE);

                installer.DoInstall();

                interlock.Dispose();

                ProcessLaunchArgs();
            }
            else
            {
#if QA_BUILD
                Frontend.ShowMessageBox("You are about to install a QA build of Ultrastrap. The red window border indicates that this is a QA build.\n\nQA builds are handled completely separately of your standard installation, like a virtual environment.", MessageBoxImage.Information);
#endif

                new LanguageSelectorDialog().ShowDialog();

                var installer = new UI.Elements.Installer.MainWindow();
                installer.ShowDialog();

                interlock.Dispose();

                ProcessNextAction(installer.CloseAction, !installer.Finished);
            }

        }

        public static void LaunchUninstaller()
        {
            using var interlock = new InterProcessLock("Uninstaller");

            if (!interlock.IsAcquired)
            {
                Frontend.ShowMessageBox(Strings.Dialog_AlreadyRunning_Uninstaller, MessageBoxImage.Stop);
                App.Terminate();
                return;
            }

            bool confirmed = false;
            bool keepData = true;

            if (App.LaunchSettings.QuietFlag.Active)
            {
                confirmed = true;
            }
            else
            {
                var dialog = new UninstallerDialog();
                dialog.ShowDialog();

                confirmed = dialog.Confirmed;
                keepData = dialog.KeepData;
            }

            if (!confirmed)
            {
                App.Terminate();
                return;
            }

            Installer.DoUninstall(keepData);

            Frontend.ShowMessageBox(Strings.Bootstrapper_SuccessfullyUninstalled, MessageBoxImage.Information);

            App.Terminate();
        }

        public static void LaunchSettings()
        {
            ShowSettings();

            if (!App.LaunchSettings.TestModeFlag.Active)
                LaunchMenu();
        }

        public static void LaunchAccountManager()
        {
            LaunchSettingsPage(typeof(AccountsPage));
        }

        public static void LaunchSettingsPage(Type pageType)
        {
            ShowSettings(pageType);

            if (!App.LaunchSettings.TestModeFlag.Active)
                LaunchMenu();
        }

        private static void ShowSettings(Type? pageType = null)
        {
            const string LOG_IDENT = "LaunchHandler::ShowSettings";

            WaitForMouseRelease();

            using var interlock = new InterProcessLock("Settings");

            if (!interlock.IsAcquired)
            {
                App.Logger.WriteLine(LOG_IDENT, "Settings window is already open");

                var process = Utilities.GetProcessesSafe().Where(x => x.MainWindowTitle == Strings.Menu_Title).FirstOrDefault();

                if (process is not null)
                    PInvoke.SetForegroundWindow((HWND)process.MainWindowHandle);

                return;
            }

            bool showAlreadyRunningWarning = pageType is null && Process.GetProcessesByName(App.ProjectName).Length > 1;
            var window = new UI.Elements.Settings.MainWindow(showAlreadyRunningWarning);

            if (pageType is not null)
                window.Loaded += (_, _) => window.Navigate(pageType);

            window.ShowDialog();
        }

        private static void WaitForMouseRelease()
        {
            int spins = 0;

            while (Mouse.LeftButton == MouseButtonState.Pressed && spins++ < 200)
            {
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                Thread.Sleep(10);
            }
        }

        public static void LaunchPlaceFromFlags()
        {
            const string LOG_IDENT = "LaunchHandler::LaunchPlaceFromFlags";

            long? placeId = RobloxLauncher.ParsePlaceId(App.LaunchSettings.PlaceFlag.Data);

            if (placeId is null)
            {
                Frontend.ShowMessageBox("Could not parse a Roblox place ID from the -place argument.", MessageBoxImage.Error);
                App.Terminate(ErrorCode.ERROR_INVALID_FUNCTION);
                return;
            }

            int copies = 1;
            if (App.LaunchSettings.CopiesFlag.Active && Int32.TryParse(App.LaunchSettings.CopiesFlag.Data, out int parsedCopies))
                copies = Math.Clamp(parsedCopies, 1, 16);

            string? jobId = App.LaunchSettings.JobFlag.Active ? App.LaunchSettings.JobFlag.Data : null;
            string? accessCode = App.LaunchSettings.AccessCodeFlag.Active ? App.LaunchSettings.AccessCodeFlag.Data : null;

            var task = Task.Run(async () =>
            {
                if (App.LaunchSettings.AllAccountsFlag.Active)
                {
                    if (AccountManager.Accounts.Count == 0)
                    {
                        Frontend.ShowMessageBox("No accounts are saved. Add some in Settings → Accounts first.", MessageBoxImage.Warning);
                        return 0;
                    }

                    return await RobloxLauncher.LaunchAccountsAsync(placeId.Value, AccountManager.Accounts, jobId, accessCode);
                }

                Account? account = null;
                string? alias = App.LaunchSettings.AccountFlag.Active
                    ? App.LaunchSettings.AccountFlag.Data
                    : (String.IsNullOrEmpty(App.Settings.Prop.ActiveAccountAlias) ? null : App.Settings.Prop.ActiveAccountAlias);

                if (!String.IsNullOrEmpty(alias))
                    account = AccountManager.GetAccount(alias);

                return await RobloxLauncher.LaunchCopiesAsync(placeId.Value, account, copies, jobId, accessCode);
            });

            int started = 0;

            try
            {
                started = task.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                Frontend.ShowMessageBox("Failed to launch Roblox.\n\n" + ex.Message, MessageBoxImage.Error);
            }

            if (started == 0 && RobloxLauncher.GetPlayerExecutablePath() is null)
            {
                App.Logger.WriteLine(LOG_IDENT, "Player missing, falling back to bootstrapper");
                App.LaunchSettings.RobloxLaunchMode = LaunchMode.Player;
                App.LaunchSettings.RobloxLaunchArgs = RobloxLauncher.BuildPlayerLaunchArgs(placeId.Value, null, jobId, accessCode);
                LaunchRoblox(LaunchMode.Player);
                return;
            }

            App.Terminate();
        }

        public static void LaunchMenu()
        {
            while (true)
            {
                WaitForMouseRelease();

                var dialog = new LaunchMenuDialog();
                dialog.ShowDialog();

                switch (dialog.CloseAction)
                {
                    case NextAction.LaunchSettings:
                        ShowSettings();
                        continue;
                    case NextAction.LaunchAccounts:
                        ShowSettings(typeof(AccountsPage));
                        continue;
                    case NextAction.LaunchQuickLaunch:
                        ShowSettings(typeof(LaunchPage));
                        continue;
                    case NextAction.LaunchInstances:
                        ShowSettings(typeof(InstancesPage));
                        continue;
                    default:
                        ProcessNextAction(dialog.CloseAction);
                        return;
                }
            }
        }

        public static void LaunchRoblox(LaunchMode launchMode)
        {
            const string LOG_IDENT = "LaunchHandler::LaunchRoblox";

            if (launchMode == LaunchMode.None)
                throw new InvalidOperationException("No Roblox launch mode set");

            if (!File.Exists(Path.Combine(Paths.System, "mfplat.dll")))
            {
                Frontend.ShowMessageBox(Strings.Bootstrapper_WMFNotFound, MessageBoxImage.Error);

                if (!App.LaunchSettings.QuietFlag.Active)
                    Utilities.ShellExecute("https://support.microsoft.com/en-us/topic/media-feature-pack-list-for-windows-n-editions-c1c6fffa-d052-8338-7a79-a4bb980a700a");

                App.Terminate(ErrorCode.ERROR_FILE_NOT_FOUND);
            }

            // ultrastrap: inject a specific account's authentication ticket if requested
            if (launchMode == LaunchMode.Player)
            {
                string? accountAlias = App.LaunchSettings.AccountFlag.Active
                    ? App.LaunchSettings.AccountFlag.Data
                    : (String.IsNullOrEmpty(App.Settings.Prop.ActiveAccountAlias) ? null : App.Settings.Prop.ActiveAccountAlias);

                if (!String.IsNullOrEmpty(accountAlias))
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Applying account '{accountAlias}' to launch");
                    string newArgs = AccountManager.ApplyAccountToLaunchArgs(App.LaunchSettings.RobloxLaunchArgs, accountAlias);
                    App.LaunchSettings.RobloxLaunchArgs = newArgs;
                }
            }

            // ultrastrap: if multi-instancing is on, hold the singleton mutex open so multiple
            // Roblox windows can run at once, and skip the "already running" confirmation.
            if (App.Settings.Prop.MultiInstanceLaunching && launchMode != LaunchMode.Studio)
            {
                App.Logger.WriteLine(LOG_IDENT, "Multi-instance launching enabled");
                MultiInstance.EnsureMutexWatcherRunning();
            }

            if (!App.Settings.Prop.MultiInstanceLaunching
                && App.Settings.Prop.ConfirmLaunches && launchMode != LaunchMode.Studio && Mutex.TryOpenExisting("ROBLOX_singletonMutex", out var _))
            {
                // this currently doesn't work very well since it relies on checking the existence of the singleton mutex
                // which often hangs around for a few seconds after the window closes
                // it would be better to have this rely on the activity tracker when we implement IPC in the planned refactoring

                var result = Frontend.ShowMessageBox(Strings.Bootstrapper_ConfirmLaunch, MessageBoxImage.Warning, MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                {
                    App.Terminate();
                    return;
                }
            }

            // start bootstrapper and show the bootstrapper modal if we're not running silently
            App.Logger.WriteLine(LOG_IDENT, "Initializing bootstrapper");
            App.Bootstrapper = new Bootstrapper(launchMode);
            IBootstrapperDialog? dialog = null;

            if (!App.LaunchSettings.QuietFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Initializing bootstrapper dialog");
                dialog = App.Settings.Prop.BootstrapperStyle.GetNew();
                App.Bootstrapper.Dialog = dialog;
                dialog.Bootstrapper = App.Bootstrapper;
            }

            Task.Run(App.Bootstrapper.Run).ContinueWith(t =>
            {
                App.Logger.WriteLine(LOG_IDENT, "Bootstrapper task has finished");

                if (t.IsFaulted)
                {
                    App.Logger.WriteLine(LOG_IDENT, "An exception occurred when running the bootstrapper");

                    if (t.Exception is not null)
                        App.FinalizeExceptionHandling(t.Exception);
                }

                App.Terminate();
            });

            dialog?.ShowBootstrapper();

            App.Logger.WriteLine(LOG_IDENT, "Exiting");
        }

        public static void LaunchWatcher()
        {
            const string LOG_IDENT = "LaunchHandler::LaunchWatcher";

            // this whole topology is a bit confusing, bear with me:
            // main thread: strictly UI only, handles showing of the notification area icon, context menu, server details dialog
            // - server information task: queries server location, invoked if either the explorer notification is shown or the server details dialog is opened
            // - discord rpc thread: handles rpc connection with discord
            //    - discord rich presence tasks: handles querying and displaying of game information, invoked on activity watcher events
            // - watcher task: runs activity watcher + waiting for roblox to close, terminates when it has

            var watcher = new Watcher();

            Task.Run(watcher.Run).ContinueWith(t => 
            {
                App.Logger.WriteLine(LOG_IDENT, "Watcher task has finished");

                watcher.Dispose();

                if (t.IsFaulted)
                {
                    App.Logger.WriteLine(LOG_IDENT, "An exception occurred when running the watcher");

                    if (t.Exception is not null)
                        App.FinalizeExceptionHandling(t.Exception);
                }

                App.Terminate();
            });
        }

        public static void LaunchBackgroundUpdater()
        {
            const string LOG_IDENT = "LaunchHandler::LaunchBackgroundUpdater";

            // Activate some LaunchFlags we need
            App.LaunchSettings.QuietFlag.Active = true;
            App.LaunchSettings.NoLaunchFlag.Active = true;

            if (!Enum.TryParse(App.LaunchSettings.BackgroundUpdaterFlag.Data, out LaunchMode launchMode))
                throw new ApplicationException($"Invalid launch mode arg ({App.LaunchSettings.BackgroundUpdaterFlag.Data})");

            if (launchMode != LaunchMode.Player && launchMode != LaunchMode.Studio)
                throw new ApplicationException($"Unsupported launch mode {launchMode} provided");

            App.Logger.WriteLine(LOG_IDENT, "Initializing bootstrapper");
            App.Bootstrapper = new Bootstrapper(launchMode)
            {
                MutexNamePrefix = "Bloxstrap-BackgroundUpdater",
                QuitIfMutexExists = true
            };

            CancellationTokenSource cts = new CancellationTokenSource();

            Task.Run(() =>
            {
                App.Logger.WriteLine(LOG_IDENT, "Started event waiter");
                using (EventWaitHandle handle = new EventWaitHandle(false, EventResetMode.AutoReset, "Bloxstrap-BackgroundUpdaterKillEvent"))
                    handle.WaitOne();

                App.Logger.WriteLine(LOG_IDENT, "Received close event, killing it all!");
                App.Bootstrapper.Cancel();
            }, cts.Token);

            Task.Run(App.Bootstrapper.Run).ContinueWith(t =>
            {
                App.Logger.WriteLine(LOG_IDENT, "Bootstrapper task has finished");
                cts.Cancel(); // stop event waiter

                if (t.IsFaulted)
                {
                    App.Logger.WriteLine(LOG_IDENT, "An exception occurred when running the bootstrapper");

                    if (t.Exception is not null)
                        App.FinalizeExceptionHandling(t.Exception);
                }

                App.Terminate();
            });

            App.Logger.WriteLine(LOG_IDENT, "Exiting");
        }
    }
}
