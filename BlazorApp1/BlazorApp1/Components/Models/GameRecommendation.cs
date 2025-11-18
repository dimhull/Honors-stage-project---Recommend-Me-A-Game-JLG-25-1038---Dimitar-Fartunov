using BlazorApp1.Models;

namespace BlazorApp1.Components.Models
{
    public class GameRecommendation
    {
        public Game Game { get; set; }
        public double SimilarityScore { get; set; }
        public List<Tag> MatchingTags { get; set; } = new();
        public string MatchReason { get; set; }
    }
}