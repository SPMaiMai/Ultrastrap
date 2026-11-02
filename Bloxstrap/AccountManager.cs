using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace Bloxstrap
{
    /// <summary>
    /// Ultrastrap account manager.
    ///
    /// Stores Roblox accounts (by their <c>.ROBLOSECURITY</c> cookie) and can generate fresh
    /// authentication tickets for them on demand. Injecting a freshly generated ticket into a
    /// Roblox launch URI lets the user launch a game as any stored account, which - combined with
    /// multi-instancing - allows several different accounts to play simultaneously.
    /// </summary>
    public static class AccountManager
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Ultrastrap.AccountManager.v1");

        public static ObservableCollection<Account> Accounts => App.Accounts.Prop.Accounts;

        #region Cookie protection (DPAPI, per-user)
        public static string Protect(string plaintext)
        {
            if (String.IsNullOrEmpty(plaintext))
                return "";

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(plaintext);
                byte[] encrypted = ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encrypted);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("AccountManager::Protect", "Failed to protect cookie");
                App.Logger.WriteException("AccountManager::Protect", ex);
                return "";
            }
        }

        public static string Unprotect(string protectedText)
        {
            if (String.IsNullOrEmpty(protectedText))
                return "";

            try
            {
                byte[] encrypted = Convert.FromBase64String(protectedText);
                byte[] data = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("AccountManager::Unprotect", "Failed to unprotect cookie");
                App.Logger.WriteException("AccountManager::Unprotect", ex);
                return "";
            }
        }
        #endregion

        public static Account? GetAccount(string alias)
        {
            return Accounts.FirstOrDefault(x => x.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));
        }

        public static void Save() => App.Accounts.Save();

        /// <summary>
        /// Validates a cookie against the Roblox API and adds (or updates) the account.
        /// Returns the resulting account, or null if the cookie is invalid.
        /// </summary>
        public static async Task<Account?> AddOrUpdateAccountAsync(string cookie, string? alias = null)
        {
            const string LOG_IDENT = "AccountManager::AddOrUpdateAccountAsync";

            cookie = CleanCookie(cookie);

            if (String.IsNullOrEmpty(cookie))
                return null;

            var info = await GetAuthenticatedUserAsync(cookie);

            if (info is null)
            {
                App.Logger.WriteLine(LOG_IDENT, "Cookie did not resolve to a valid user");
                return null;
            }

            string finalAlias = String.IsNullOrWhiteSpace(alias) ? info.Value.name : alias!.Trim();

            var existing = Accounts.FirstOrDefault(x => x.UserId == info.Value.id);
            var account = existing ?? new Account();

            account.Alias = finalAlias;
            account.UserId = info.Value.id;
            account.Username = info.Value.name;
            account.DisplayName = info.Value.displayName;
            account.Cookie = cookie;

            if (existing is null)
                Accounts.Add(account);

            Save();

            App.Logger.WriteLine(LOG_IDENT, $"Added/updated account '{account.Alias}' (user {account.UserId})");

            return account;
        }

        public static void RemoveAccount(Account account)
        {
            Accounts.Remove(account);

            if (App.Settings.Prop.ActiveAccountAlias.Equals(account.Alias, StringComparison.OrdinalIgnoreCase))
            {
                App.Settings.Prop.ActiveAccountAlias = "";
                App.Settings.Save();
            }

            Save();
        }

        private static string CleanCookie(string cookie)
        {
            cookie = cookie.Trim();

            // allow pasting the full "Cookie:" header or just the value
            int idx = cookie.IndexOf(".ROBLOSECURITY=", StringComparison.OrdinalIgnoreCase);
            if (idx != -1)
            {
                cookie = cookie[(idx + ".ROBLOSECURITY=".Length)..];
                int end = cookie.IndexOf(';');
                if (end != -1)
                    cookie = cookie[..end];
            }

            return cookie.Trim();
        }

        #region Roblox web API
        private static HttpClient CreateCookieClient(string cookie)
        {
            var handler = new HttpClientHandler
            {
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.All
            };

            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("User-Agent", "Roblox/WinInet");
            client.DefaultRequestHeaders.Add("Cookie", $".ROBLOSECURITY={cookie}");
            client.DefaultRequestHeaders.Add("Referer", "https://www.roblox.com/");
            client.DefaultRequestHeaders.Add("Origin", "https://www.roblox.com");
            return client;
        }

        private static async Task<(long id, string name, string displayName)?> GetAuthenticatedUserAsync(string cookie)
        {
            const string LOG_IDENT = "AccountManager::GetAuthenticatedUserAsync";

            try
            {
                using var client = CreateCookieClient(cookie);
                using var resp = await client.GetAsync("https://users.roblox.com/v1/users/authenticated");

                if (!resp.IsSuccessStatusCode)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Request failed ({(int)resp.StatusCode})");
                    return null;
                }

                string json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return (
                    root.GetProperty("id").GetInt64(),
                    root.GetProperty("name").GetString() ?? "",
                    root.TryGetProperty("displayName", out var dn) ? (dn.GetString() ?? "") : ""
                );
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                return null;
            }
        }

        /// <summary>
        /// Generates a fresh authentication ticket for the given cookie, following Roblox's
        /// CSRF-then-ticket flow.
        /// </summary>
        public static async Task<string?> GenerateAuthTicketAsync(string cookie)
        {
            const string LOG_IDENT = "AccountManager::GenerateAuthTicketAsync";

            const string ticketUrl = "https://auth.roblox.com/v1/authentication-ticket/";

            try
            {
                using var client = CreateCookieClient(cookie);

                // 1. prime the CSRF token (Roblox returns it in a 403 response header)
                string csrfToken = "";
                using (var primeReq = new HttpRequestMessage(HttpMethod.Post, ticketUrl))
                using (var primeResp = await client.SendAsync(primeReq))
                {
                    if (primeResp.Headers.TryGetValues("x-csrf-token", out var values))
                        csrfToken = values.FirstOrDefault() ?? "";
                }

                if (String.IsNullOrEmpty(csrfToken))
                {
                    App.Logger.WriteLine(LOG_IDENT, "Failed to obtain CSRF token");
                    return null;
                }

                // 2. request the authentication ticket
                using var req = new HttpRequestMessage(HttpMethod.Post, ticketUrl);
                req.Headers.Add("X-CSRF-TOKEN", csrfToken);
                req.Content = new StringContent("", Encoding.UTF8, "application/json");

                using var resp = await client.SendAsync(req);

                if (resp.Headers.TryGetValues("rbx-authentication-ticket", out var ticketValues))
                {
                    string? ticket = ticketValues.FirstOrDefault();
                    App.Logger.WriteLine(LOG_IDENT, String.IsNullOrEmpty(ticket) ? "Empty ticket returned" : "Generated authentication ticket");
                    return ticket;
                }

                App.Logger.WriteLine(LOG_IDENT, $"No ticket header in response ({(int)resp.StatusCode})");
                return null;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                return null;
            }
        }
        #endregion

        /// <summary>
        /// Injects a fresh authentication ticket for the given account into a Roblox launch URI,
        /// so the launch is performed as that account. Returns the original args unchanged on failure.
        /// </summary>
        public static string ApplyAccountToLaunchArgs(string launchArgs, string alias)
        {
            const string LOG_IDENT = "AccountManager::ApplyAccountToLaunchArgs";

            if (String.IsNullOrEmpty(launchArgs))
                return launchArgs;

            var account = GetAccount(alias);

            if (account is null)
            {
                App.Logger.WriteLine(LOG_IDENT, $"No stored account with alias '{alias}'");
                return launchArgs;
            }

            string? ticket = GenerateAuthTicketAsync(account.Cookie).GetAwaiter().GetResult();

            if (String.IsNullOrEmpty(ticket))
            {
                App.Logger.WriteLine(LOG_IDENT, "Could not generate ticket; launch will use the default account");
                return launchArgs;
            }

            string result = launchArgs;

            if (Regex.IsMatch(result, "gameinfo:", RegexOptions.IgnoreCase))
                result = Regex.Replace(result, "gameinfo:[^+]*", "gameinfo:" + ticket, RegexOptions.IgnoreCase);
            else
                result += (result.EndsWith('+') ? "" : "+") + "gameinfo:" + ticket;

            if (Regex.IsMatch(result, "launchtime:", RegexOptions.IgnoreCase))
                result = Regex.Replace(result, "launchtime:[0-9]+", "launchtime:" + DateTimeOffset.Now.ToUnixTimeMilliseconds(), RegexOptions.IgnoreCase);

            return result;
        }
    }
}
