namespace Bloxstrap.Models
{
    /// <summary>
    /// A saved Roblox experience the user can one-click launch.
    /// </summary>
    public class FavoritePlace
    {
        public string Name { get; set; } = "";

        public long PlaceId { get; set; }

        public string? JobId { get; set; }

        public string? AccessCode { get; set; }

        public DateTime LastUsed { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public string DisplayLabel
        {
            get
            {
                string extra = "";
                if (!String.IsNullOrEmpty(AccessCode))
                    extra = " (private)";
                else if (!String.IsNullOrEmpty(JobId))
                    extra = " (specific server)";

                return $"{Name}  ·  {PlaceId}{extra}";
            }
        }
    }
}
