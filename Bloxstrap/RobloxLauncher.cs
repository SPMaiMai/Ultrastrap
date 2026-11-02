using System.Web;
using Bloxstrap.AppData;

namespace Bloxstrap
{
    /// <summary>
    /// Builds Roblox player launch URIs and starts one or more client windows.
    /// Used for place-ID launches, multi-copy same-account launches, and multi-account launches.
    /// </summary>
    public static class RobloxLauncher
    {
        public static string? GetPlayerExecutablePath()
        {
            try
            {
                string path = new RobloxPlayerData().ExecutablePath;
                return File.Exists(path) ? path : null;
            }
            catch
            {
                return null;
            }
        }

        public static long? ParsePlaceId(string? input)
        {
            if (String.IsNullOrWhiteSpace(input))
                return null;

            input = input.Trim();

            if (Int64.TryParse(input, out long numeric) && numeric > 0)
                return numeric;

            var gamesMatch = Regex.Match(input, @"roblox\.com/games/(\d+)", RegexOptions.IgnoreCase);
            if (gamesMatch.Success && Int64.TryParse(gamesMatch.Groups[1].Value, out long fromGames))
                return fromGames;

            try
            {
                if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
                {
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    string? placeId = query["placeId"] ?? query["placeid"];
                    if (Int64.TryParse(placeId, out long fromQuery) && fromQuery > 0)
                        return fromQuery;
                }
            }
            catch
            {
                // ignore malformed URIs
            }

            return null;
        }

        public static string BuildPlayerLaunchArgs(long placeId, string? ticket = null, string? jobId = null, string? accessCode = null)
        {
            long tracker = Random.Shared.NextInt64(10_000_000_000, 9_000_000_000_000);
            string request;

            if (!String.IsNullOrEmpty(accessCode))
                request = "RequestPrivateGame";
            else if (!String.IsNullOrEmpty(jobId))
                request = "RequestGameJob";
            else
                request = "RequestGame";

            var query = new StringBuilder();
            query.Append("request=").Append(request);
            query.Append("&browserTrackerId=").Append(tracker);
            query.Append("&placeId=").Append(placeId);
            query.Append("&isPlayTogetherGame=false");

            if (!String.IsNullOrEmpty(jobId))
                query.Append("&gameId=").Append(HttpUtility.UrlEncode(jobId));

            if (!String.IsNullOrEmpty(accessCode))
                query.Append("&accessCode=").Append(HttpUtility.UrlEncode(accessCode));

            string launcherUrl = "https://assetgame.roblox.com/game/PlaceLauncher.ashx?" + query;
            long launchTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            return "roblox-player:1+launchmode:play"
                 + "+gameinfo:" + (ticket ?? "")
                 + "+launchtime:" + launchTime
                 + "+placelauncherurl:" + HttpUtility.UrlEncode(launcherUrl)
                 + "+browsertrackerid:" + tracker
                 + "+robloxLocale:en_us+gameLocale:en_us+channel:+LaunchExp:InApp";
        }

        public static async Task<string> BuildArgsForAccountAsync(long placeId, Account? account, string? jobId = null, string? accessCode = null)
        {
            string? ticket = null;

            if (account is not null && !String.IsNullOrEmpty(account.Cookie))
                ticket = await AccountManager.GenerateAuthTicketAsync(account.Cookie);

            return BuildPlayerLaunchArgs(placeId, ticket, jobId, accessCode);
        }

        /// <summary>
        /// Starts one Roblox player process with the given launch arguments.
        /// </summary>
        public static Process? StartPlayer(string launchArgs)
        {
            const string LOG_IDENT = "RobloxLauncher::StartPlayer";

            string? exe = GetPlayerExecutablePath();

            if (String.IsNullOrEmpty(exe))
            {
                App.Logger.WriteLine(LOG_IDENT, "Roblox player is not installed yet");
                return null;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = launchArgs,
                    WorkingDirectory = Path.GetDirectoryName(exe),
                    UseShellExecute = false
                };

                var process = Process.Start(startInfo);
                App.Logger.WriteLine(LOG_IDENT, $"Started Roblox player (PID {process?.Id})");
                return process;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to start Roblox player");
                App.Logger.WriteException(LOG_IDENT, ex);
                return null;
            }
        }

        public static void PrepareMultiInstance()
        {
            if (!App.Settings.Prop.MultiInstanceLaunching)
            {
                App.Settings.Prop.MultiInstanceLaunching = true;
                App.Settings.Save();
            }

            MultiInstance.EnsureMutexWatcherRunning();
        }

        public record LaunchRequest(
            long PlaceId,
            Account? Account = null,
            string? JobId = null,
            string? AccessCode = null,
            string? WindowLabel = null
        );

        /// <summary>
        /// Launches one or more Roblox windows, generating a fresh auth ticket per window.
        /// Automatically enables multi-instancing when more than one window is requested
        /// or when a Roblox client is already running.
        /// </summary>
        public static async Task<int> LaunchManyAsync(IReadOnlyList<LaunchRequest> requests, IProgress<string>? progress = null)
        {
            const string LOG_IDENT = "RobloxLauncher::LaunchManyAsync";

            if (requests.Count == 0)
                return 0;

            if (GetPlayerExecutablePath() is null)
            {
                progress?.Report("Roblox is not installed yet. Launch Roblox once from the main menu first.");
                return 0;
            }

            bool needsMulti = requests.Count > 1 || MultiInstance.AnyRobloxRunning();

            if (needsMulti)
                PrepareMultiInstance();

            App.Settings.Prop.LastPlaceId = requests[0].PlaceId.ToString();
            App.Settings.Save();

            int started = 0;
            int stagger = Math.Clamp(App.Settings.Prop.LaunchStaggerMs, 250, 10_000);
            var labels = new List<string>();

            for (int i = 0; i < requests.Count; i++)
            {
                var request = requests[i];
                string label = request.WindowLabel
                    ?? request.Account?.Alias
                    ?? request.Account?.Username
                    ?? $"window {i + 1}";

                progress?.Report($"Launching {label} ({i + 1}/{requests.Count})…");
                App.Logger.WriteLine(LOG_IDENT, $"Launch {i + 1}/{requests.Count} as '{label}' into place {request.PlaceId}");

                string args = await BuildArgsForAccountAsync(request.PlaceId, request.Account, request.JobId, request.AccessCode);
                var process = StartPlayer(args);

                if (process is not null)
                {
                    started++;
                    labels.Add(label);
                }
                else
                {
                    labels.Add("");
                }

                if (i < requests.Count - 1)
                    await Task.Delay(stagger);
            }

            if (started > 0 && (App.Settings.Prop.AutoTileWindows || App.Settings.Prop.RenameWindowsByAccount))
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(Math.Max(4000, stagger));
                    TryArrangeLaunchedWindows(labels);
                });
            }

            progress?.Report(started == 0
                ? "No Roblox windows were started."
                : $"Started {started} Roblox window(s).");

            return started;
        }

        public static Task<int> LaunchCopiesAsync(long placeId, Account? account, int copies, string? jobId = null, string? accessCode = null, IProgress<string>? progress = null)
        {
            copies = Math.Clamp(copies, 1, 16);

            var requests = Enumerable.Range(1, copies)
                .Select(i => new LaunchRequest(
                    placeId,
                    account,
                    jobId,
                    accessCode,
                    copies == 1
                        ? (account?.Alias ?? account?.Username)
                        : $"{account?.Alias ?? account?.Username ?? "Roblox"} #{i}"
                ))
                .ToList();

            return LaunchManyAsync(requests, progress);
        }

        public static Task<int> LaunchAccountsAsync(long placeId, IEnumerable<Account> accounts, string? jobId = null, string? accessCode = null, IProgress<string>? progress = null)
        {
            var requests = accounts
                .Select(account => new LaunchRequest(placeId, account, jobId, accessCode, account.Alias))
                .ToList();

            return LaunchManyAsync(requests, progress);
        }

        private static void TryArrangeLaunchedWindows(List<string> labels)
        {
            try
            {
                var instances = MultiInstance.GetRunningInstances();

                if (instances.Count == 0)
                    return;

                // newest windows first, then oldest — match launch order as well as we can
                var recent = instances
                    .OrderByDescending(x => x.StartTime)
                    .Take(labels.Count)
                    .Reverse()
                    .ToList();

                WindowLayout.ApplyAccountTitles(recent, labels);

                if (App.Settings.Prop.AutoTileWindows && recent.Count > 1)
                    WindowLayout.Tile(recent, WindowTileMode.Grid);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("RobloxLauncher::TryArrangeLaunchedWindows", ex);
            }
        }
    }
}
