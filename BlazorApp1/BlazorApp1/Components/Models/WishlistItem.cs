namespace BlazorApp1.Components.Models
{
    public class WishlistItem
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        // Game info from RAWG
        public int GameId { get; set; }

        public string GameName { get; set; }
        public string GameImage { get; set; }
        public double GameRating { get; set; }
        public DateTime? GameReleased { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}