using System.Collections.ObjectModel;

namespace Bloxstrap.Models.Persistable
{
    /// <summary>
    /// Persisted collection of Roblox accounts managed by Ultrastrap (Accounts.json).
    /// </summary>
    public class AccountData
    {
        public ObservableCollection<Account> Accounts { get; set; } = new();
    }
}
