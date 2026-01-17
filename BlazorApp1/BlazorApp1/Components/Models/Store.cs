using System.Text.Json.Serialization;

namespace BlazorApp1.Components.Models
{
    public class StoreInfo
    {
        public int Id { get; set; }
        public Store Store { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class Store
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Domain { get; set; }

        [JsonPropertyName("image_background")]
        public string ImageBackground { get; set; }
    }
}