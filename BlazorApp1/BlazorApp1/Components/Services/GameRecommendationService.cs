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
        var recs = new List<GameRecommendation>();

        foreach (var cand in candidates)
        {
            if (cand.Id == source.Id) continue;

            // --- THE SCORING ALGORITHM ---

            // A. Tag Similarity (Jaccard)
            var matchingTags = cand.Tags.IntersectBy(source.Tags.Select(t => t.Id), t => t.Id).ToList();
            double similarity = (double)matchingTags.Count / (source.Tags.Count + cand.Tags.Count - matchingTags.Count);

            // B. Popularity Penalty
            // Log10 of AddedCount smooths out the difference between 100k players and 1k players
            // We divide by this so "Mega-hits" have their score lowered slightly
            double popularityFactor = Math.Log10(cand.AddedCount + 2);

            // C. Final Weighted Score
            // We multiply by rating to ensure the hidden gems are actually GOOD games
            double finalScore = (similarity * (cand.Rating / 100.0)) / popularityFactor;

            recs.Add(new GameRecommendation
            {
                Game = cand,
                SimilarityScore = finalScore,
                MatchingTags = matchingTags,
                MatchReason = matchingTags.Count > 3 ? "Highly similar gameplay" : "Similar themes"
            });
        }

        return recs.OrderByDescending(r => r.SimilarityScore).Take(max).ToList();
    }
}