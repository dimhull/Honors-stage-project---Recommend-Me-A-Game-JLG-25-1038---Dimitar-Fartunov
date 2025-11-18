using BlazorApp1.Components.Models;

namespace BlazorApp1.Models
{
    public class GameRecommendationService
    {
        private readonly RawgApiService _apiService;

        public GameRecommendationService(RawgApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<List<GameRecommendation>> GetRecommendationsAsync(Game sourceGame, int maxResults = 10)
        {
            if (sourceGame == null || sourceGame.Tags == null || !sourceGame.Tags.Any())
            {
                return new List<GameRecommendation>();
            }

            // Extract tag IDs from source game
            var sourceTagIds = sourceGame.Tags.Select(t => t.Id).ToList();

            // Get games with similar tags
            var candidateGames = await _apiService.GetGamesByTagsAsync(sourceTagIds, 40);

            // Calculate recommendations
            var recommendations = new List<GameRecommendation>();

            foreach (var candidate in candidateGames)
            {
                // Skip the source game itself
                if (candidate.Id == sourceGame.Id)
                    continue;

                // Find matching tags
                var matchingTags = candidate.Tags
                    .Where(t => sourceGame.Tags.Any(st => st.Id == t.Id))
                    .ToList();

                if (!matchingTags.Any())
                    continue;

                // Calculate similarity score using Jaccard similarity
                var intersection = matchingTags.Count;
                var union = sourceGame.Tags.Count + candidate.Tags.Count - intersection;
                var similarityScore = (double)intersection / union;

                // Weight by rating (optional boost)
                var adjustedScore = similarityScore * (1 + (candidate.Rating / 100));

                recommendations.Add(new GameRecommendation
                {
                    Game = candidate,
                    SimilarityScore = adjustedScore,
                    MatchingTags = matchingTags,
                    MatchReason = $"{matchingTags.Count} matching tags"
                });
            }

            // Sort by score and return top results
            return recommendations
                .OrderByDescending(r => r.SimilarityScore)
                .Take(maxResults)
                .ToList();
        }
    }
}