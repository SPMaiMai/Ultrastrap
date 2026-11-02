namespace Bloxstrap.Models
{
    /// <summary>
    /// A stored Roblox account used by the Ultrastrap account manager.
    /// The sensitive <c>.ROBLOSECURITY</c> cookie is stored DPAPI-encrypted (per Windows user).
    /// </summary>
    public class Account
    {
        /// <summary>
        /// A friendly name the user assigns to this account (defaults to the Roblox username).
        /// Used as the identifier when launching with -account.
        /// </summary>
        public string Alias { get; set; } = "";

        /// <summary>
        /// DPAPI-protected, base64-encoded .ROBLOSECURITY cookie. Do not read directly; use <see cref="Cookie"/>.
        /// </summary>
        public string ProtectedCookie { get; set; } = "";

        public long UserId { get; set; } = 0;

        public string Username { get; set; } = "";

        public string DisplayName { get; set; } = "";

        /// <summary>
        /// The decrypted .ROBLOSECURITY cookie. Not serialised to disk.
        /// </summary>
        [JsonIgnore]
        public string Cookie
        {
            get => AccountManager.Unprotect(ProtectedCookie);
            set => ProtectedCookie = AccountManager.Protect(value);
        }

        [JsonIgnore]
        public string DisplayLabel => String.IsNullOrEmpty(Username) ? Alias : $"{Alias} ({Username})";
    }
}
