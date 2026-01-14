using Microsoft.Extensions.Caching.Memory;
using BlazorApp1.Components.Models;
using BlazorApp1.Components.Service;

public class GameRecommendationService
{
    private readonly RawgApiService _apiService;
    private readonly IMemoryCache _cache;

    public GameRecommendationService(RawgApiService apiService, IMemoryCache cache)
    {
        _apiService = apiService;
        _cache = cache;
    }

    public async Task<List<GameRecommendation>> GetRecommendationsAsync(Game sourceGame, int maxResults = 10)
    {
        if (sourceGame?.Tags == null || !sourceGame.Tags.Any()) return new();

        string cacheKey = $"recs_{sourceGame.Id}";

        // Check if we already have recommendations for this game in memory
        if (!_cache.TryGetValue(cacheKey, out List<GameRecommendation> cachedRecs))
        {
            var sourceTagIds = sourceGame.Tags.Select(t => t.Id).ToList();

            // 1. Fetch more data (3 pages = 120 candidates)
            var tasks = Enumerable.Range(1, 3)
                .Select(page => _apiService.GetGamesByTagsAsync(sourceTagIds, 40, page));

            var results = await Task.WhenAll(tasks);
            var candidates = results.SelectMany(g => g).DistinctBy(g => g.Id).ToList();

            cachedRecs = ProcessRecommendations(sourceGame, candidates, maxResults);

            // 2. Save to cache for 60 minutes
            _cache.Set(cacheKey, cachedRecs, TimeSpan.FromMinutes(60));
        }

        return cachedRecs;
    }

    private List<GameRecommendation> ProcessRecommendations(Game source, List<Game> candidates, int max)
    {
        Console.WriteLine($"\n=== PROCESSING RECOMMENDATIONS FOR: {source.Name} ===");
        Console.WriteLine($"Source game has {source.Tags.Count} tags");
        Console.WriteLine($"Processing {candidates.Count} candidate games...\n");

        var recs = new List<GameRecommendation>();

        foreach (var cand in candidates)
        {
            if (cand.Id == source.Id) continue;
            if (cand.Tags == null || !cand.Tags.Any()) continue;

            var matchingTags = cand.Tags
                .Where(t => source.Tags.Any(st => st.Id == t.Id))
                .ToList();

            // Require at least 4 matching tags
            if (matchingTags.Count < 4) continue;

            // Simple overlap score (what % of source tags match)
            double overlapScore = (double)matchingTags.Count / source.Tags.Count;

            // Jaccard similarity
            int intersection = matchingTags.Count;
            int union = source.Tags.Count + cand.Tags.Count - intersection;
            double jaccardScore = (double)intersection / union;

            // Combine: 60% overlap + 40% Jaccard
            double similarity = (overlapScore * 0.6) + (jaccardScore * 0.4);

            // Very light rating boost (max 10%) to prefer quality games
            double ratingBoost = 1 + ((cand.Rating - 3.0) / 50.0);

            double finalScore = similarity * ratingBoost;

            recs.Add(new GameRecommendation
            {
                Game = cand,
                SimilarityScore = finalScore,
                MatchingTags = matchingTags,
                MatchReason = $"{matchingTags.Count}/{source.Tags.Count} tags match"
            });
        }

        Console.WriteLine($"\n✓ Found {recs.Count} qualifying games (4+ matching tags)");

        return recs
            .OrderByDescending(r => r.MatchingTags.Count)  // Primary: Most matching tags
            .ThenByDescending(r => r.SimilarityScore)      // Secondary: Similarity score
            .Take(max)
            .ToList();
    }
}