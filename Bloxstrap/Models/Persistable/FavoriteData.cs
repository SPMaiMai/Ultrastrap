using System.Collections.ObjectModel;

namespace Bloxstrap.Models.Persistable
{
    /// <summary>
    /// Persisted favorite places (Favorites.json).
    /// </summary>
    public class FavoriteData
    {
        public ObservableCollection<FavoritePlace> Places { get; set; } = new();
    }
}
