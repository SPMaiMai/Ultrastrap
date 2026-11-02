namespace Bloxstrap
{
    /// <summary>
    /// Ultrastrap multi-instance support.
    ///
    /// Roblox only allows a single running instance by creating named mutexes
    /// (<c>ROBLOX_singletonMutex</c> and <c>ROBLOX_singletonEvent</c>) on startup.
    /// If a long-lived process holds those mutexes open, Roblox stops enforcing the
    /// single-instance restriction, which lets the user open as many Roblox windows
    /// (same or different accounts) as they like.
    ///
    /// We hold the mutexes inside a dedicated, self-singleton background process so
    /// they stay alive independently of any individual launch.
    /// </summary>
    public static class MultiInstance
    {
        public const string SingletonMutexName = "ROBLOX_singletonMutex";
        public const string SingletonEventName = "ROBLOX_singletonEvent";
        public const string WatcherStopEventName = "Ultrastrap-MultiInstanceWatcherStop";

        private static readonly string[] RobloxProcessNames =
        {
            "RobloxPlayerBeta",
            "RobloxStudioBeta"
        };

        /// <summary>
        /// Spawns the background mutex holder process if multi-instancing is enabled and it isn't
        /// already running. Safe to call on every launch; the watcher self-singletons.
        /// </summary>
        public static void EnsureMutexWatcherRunning()
        {
            const string LOG_IDENT = "MultiInstance::EnsureMutexWatcherRunning";

            if (!App.Settings.Prop.MultiInstanceLaunching)
                return;

            if (Utilities.DoesMutexExist("Ultrastrap-MultiInstanceWatcher"))
            {
                App.Logger.WriteLine(LOG_IDENT, "Mutex watcher is already running");
                return;
            }

            try
            {
                App.Logger.WriteLine(LOG_IDENT, "Spawning multi-instance mutex watcher");

                Process.Start(new ProcessStartInfo
                {
                    FileName = Paths.Process,
                    Arguments = "-multiinstancewatcher",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to spawn mutex watcher");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public static void StopMutexWatcher()
        {
            try
            {
                using var stop = new EventWaitHandle(false, EventResetMode.AutoReset, WatcherStopEventName);
                stop.Set();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("MultiInstance::StopMutexWatcher", ex);
            }
        }

        /// <summary>
        /// Entry point for the background watcher process (launched with -multiinstancewatcher).
        /// Holds the Roblox singleton mutexes until no Roblox client remains running
        /// (or indefinitely if PersistMutexWatcher is enabled).
        /// </summary>
        public static void RunMutexWatcher()
        {
            const string LOG_IDENT = "MultiInstance::RunMutexWatcher";

            using var interlock = new InterProcessLock("MultiInstanceWatcher");

            if (!interlock.IsAcquired)
            {
                App.Logger.WriteLine(LOG_IDENT, "Another mutex watcher already owns the lock, exiting");
                return;
            }

            App.Logger.WriteLine(LOG_IDENT, "Acquiring Roblox singleton mutexes");

            // hold both names Roblox has used historically so either check is satisfied
            using var singletonMutex = new Mutex(true, SingletonMutexName, out bool createdMutex);
            using var singletonEvent = new Mutex(true, SingletonEventName, out bool createdEvent);

            App.Logger.WriteLine(LOG_IDENT, createdMutex
                ? "Created ROBLOX_singletonMutex"
                : "Co-holding ROBLOX_singletonMutex");
            App.Logger.WriteLine(LOG_IDENT, createdEvent
                ? "Created ROBLOX_singletonEvent"
                : "Co-holding ROBLOX_singletonEvent");

            using var stop = new EventWaitHandle(false, EventResetMode.AutoReset, WatcherStopEventName);

            const int graceSeconds = 60;
            int waited = 0;

            while (!AnyRobloxRunning() && waited < graceSeconds)
            {
                if (stop.WaitOne(1000))
                {
                    App.Logger.WriteLine(LOG_IDENT, "Stop signal received during grace period");
                    return;
                }

                waited++;
            }

            App.Logger.WriteLine(LOG_IDENT, $"Entering watch loop (grace waited {waited}s, roblox running={AnyRobloxRunning()})");

            while (true)
            {
                if (stop.WaitOne(2000))
                {
                    App.Logger.WriteLine(LOG_IDENT, "Stop signal received");
                    break;
                }

                try
                {
                    App.Settings.Load(false);
                }
                catch
                {
                    // keep the last known settings if the file is mid-write
                }

                bool persist = App.Settings.Prop.PersistMutexWatcher && App.Settings.Prop.MultiInstanceLaunching;

                if (persist)
                    continue;

                if (!AnyRobloxRunning())
                {
                    if (stop.WaitOne(3000))
                    {
                        App.Logger.WriteLine(LOG_IDENT, "Stop signal received during settle window");
                        break;
                    }

                    if (!AnyRobloxRunning())
                        break;
                }
            }

            App.Logger.WriteLine(LOG_IDENT, "Releasing singleton mutexes");
        }

        public static bool AnyRobloxRunning() => GetRunningInstances().Count > 0;

        public static List<RobloxInstanceInfo> GetRunningInstances()
        {
            var results = new List<RobloxInstanceInfo>();

            foreach (var process in Utilities.GetProcessesSafe())
            {
                try
                {
                    bool isRoblox = false;

                    foreach (string name in RobloxProcessNames)
                    {
                        if (process.ProcessName.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            isRoblox = true;
                            break;
                        }
                    }

                    if (!isRoblox || process.HasExited)
                        continue;

                    results.Add(new RobloxInstanceInfo
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        WindowTitle = SafeMainWindowTitle(process),
                        StartTime = SafeStartTime(process),
                        WorkingSetBytes = SafeWorkingSet(process),
                        MainWindowHandle = process.MainWindowHandle
                    });
                }
                catch
                {
                    // process may have exited between enumeration and access
                }
                finally
                {
                    process.Dispose();
                }
            }

            return results.OrderBy(x => x.StartTime).ToList();
        }

        public static int CloseInstance(int processId)
        {
            const string LOG_IDENT = "MultiInstance::CloseInstance";

            try
            {
                using var process = Process.GetProcessById(processId);

                if (!IsRobloxProcessName(process.ProcessName))
                    return 0;

                process.Kill();
                App.Logger.WriteLine(LOG_IDENT, $"Closed PID {processId}");
                return 1;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to close PID {processId}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Kills every running Roblox client. Used by the "close all instances" tray action.
        /// </summary>
        public static int CloseAllRobloxInstances()
        {
            const string LOG_IDENT = "MultiInstance::CloseAllRobloxInstances";

            int killed = 0;

            foreach (var instance in GetRunningInstances())
                killed += CloseInstance(instance.ProcessId);

            App.Logger.WriteLine(LOG_IDENT, $"Closed {killed} Roblox client(s)");

            return killed;
        }

        public static int TileRunningWindows(WindowTileMode mode = WindowTileMode.Grid)
        {
            return WindowLayout.Tile(GetRunningInstances(), mode);
        }

        public static bool FocusInstance(RobloxInstanceInfo instance)
        {
            if (instance.MainWindowHandle != 0)
                return WindowLayout.BringToFront(instance.MainWindowHandle);

            try
            {
                using var process = Process.GetProcessById(instance.ProcessId);
                return WindowLayout.BringToFront(process.MainWindowHandle);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsRobloxProcessName(string name)
        {
            foreach (string expected in RobloxProcessNames)
            {
                if (name.Equals(expected, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string SafeMainWindowTitle(Process process)
        {
            try
            {
                return process.MainWindowTitle;
            }
            catch
            {
                return "";
            }
        }

        private static DateTime SafeStartTime(Process process)
        {
            try
            {
                return process.StartTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static long SafeWorkingSet(Process process)
        {
            try
            {
                return process.WorkingSet64;
            }
            catch
            {
                return 0;
            }
        }
    }
}
