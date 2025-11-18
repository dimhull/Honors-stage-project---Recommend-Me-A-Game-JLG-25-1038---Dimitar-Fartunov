namespace BlazorApp1.Components.Models
{
    public class RawgApiResponse<T>
    {
        public int Count { get; set; }
        public string Next { get; set; }
        public string Previous { get; set; }
        public List<T> Results { get; set; } = new();
    }
}
