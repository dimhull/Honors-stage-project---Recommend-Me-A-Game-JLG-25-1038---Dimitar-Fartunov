using System.Text.Json.Serialization;

namespace BlazorApp1.Models
{
    public class Game
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [JsonPropertyName("background_image")]
        public string BackgroundImage { get; set; }

        public double Rating { get; set; }
        public DateTime? Released { get; set; }
        public List<Tag> Tags { get; set; } = new();
        public List<Genre> Genres { get; set; } = new();

        [JsonPropertyName("platforms")]
        public List<PlatformInfo> Platforms { get; set; } = new();
    }
}
