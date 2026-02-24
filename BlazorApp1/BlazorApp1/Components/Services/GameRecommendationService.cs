using BlazorApp1.Components.Models;
using BlazorApp1.Components.Service;
using Microsoft.Extensions.Caching.Memory;

public class GameRecommendationService
{
    private readonly RawgApiService _apiService;
    private readonly IMemoryCache _cache;

    public GameRecommendationService(RawgApiService apiService, IMemoryCache cache)
    {
        _apiService = apiService;
        _cache = cache;
    }

    public async Task<List<GameRecommendation>> GetRecommendationsAsync(Game sourceGame, int maxResults = 12)
    {
        if (sourceGame?.Tags == null || !sourceGame.Tags.Any())
            return new List<GameRecommendation>();

        string cacheKey = $"recs_{sourceGame.Id}";

        // Check cache
        if (_cache.TryGetValue(cacheKey, out List<GameRecommendation> cachedRecs))
        {
            Console.WriteLine("✓ Using cached recommendations");
            return cachedRecs;
        }

        ContentFilter.CleanSingleGameTags(sourceGame);

        Console.WriteLine($"\n=== FINDING RECOMMENDATIONS FOR: {sourceGame.Name} ===");
        Console.WriteLine($"Source has {sourceGame.Tags.Count} English tags: {string.Join(", ", sourceGame.Tags.Take(5).Select(t => t.Name))}");

        var sourceTagIds = sourceGame.Tags.Select(t => t.Id).ToList();

        var candidates = await FetchLargeCandidatePool(sourceTagIds);

        // This removes NSFW games AND strips non-English tags from all candidates
        var safeCandidates = ContentFilter.FilterAndClean(candidates);

        var filteredCount = candidates.Count - safeCandidates.Count;
        if (filteredCount > 0)
        {
            Console.WriteLine($"🔒 Filtered out {filteredCount} NSFW games");
        }

        Console.WriteLine($"→ Processing {safeCandidates.Count} candidate games...");

        // 3. Process with STRICT rules (4+ matching tags)
        var recommendations = ProcessRecommendations(sourceGame, safeCandidates, maxResults, minMatchingTags: 4);

        Console.WriteLine($"✓ Found {recommendations.Count} high-quality recommendations\n");

        // Cache for 240 hours
        _cache.Set(cacheKey, recommendations, TimeSpan.FromHours(240));

        return recommendations;
    }

    private async Task<List<Game>> FetchLargeCandidatePool(List<int> tagIds)
    {
        const int TOTAL_PAGES = 15; // 600 games — RAWG returns by relevance so first pages are highest quality
        const int PAGE_SIZE = 40;
        const int BATCH_SIZE = 5;

        Console.WriteLine($"Fetching {TOTAL_PAGES} pages ({TOTAL_PAGES * PAGE_SIZE} games total)...");

        var allCandidates = new List<Game>();

        for (int batchStart = 1; batchStart <= TOTAL_PAGES; batchStart += BATCH_SIZE)
        {
            var batchEnd = Math.Min(batchStart + BATCH_SIZE - 1, TOTAL_PAGES);
            var batchTasks = Enumerable.Range(batchStart, batchEnd - batchStart + 1)
                .Select(page => _apiService.GetGamesByTagsAsync(tagIds, PAGE_SIZE, page));

            var batchResults = await Task.WhenAll(batchTasks);
            allCandidates.AddRange(batchResults.SelectMany(g => g));

            Console.WriteLine($"  Fetched pages {batchStart}-{batchEnd} (Total: {allCandidates.Count})");
        }

        var uniqueCandidates = allCandidates.DistinctBy(g => g.Id).ToList();
        Console.WriteLine($"✓ Total unique games fetched: {uniqueCandidates.Count}");

        return uniqueCandidates;
    }

    public List<GameRecommendation> ProcessRecommendations(Game source, List<Game> candidates, int max, int minMatchingTags)
    {
        var recs = new List<GameRecommendation>();
        var sourceTagIdSet = new HashSet<int>(source.Tags.Select(t => t.Id));

        foreach (var cand in candidates)
        {
            if (cand.Id == source.Id) continue;
            if (cand.Tags == null || !cand.Tags.Any()) continue;

            var matchingTags = cand.Tags.Where(t => sourceTagIdSet.Contains(t.Id)).ToList();

            if (matchingTags.Count < minMatchingTags) continue;

            double overlapScore = (double)matchingTags.Count / source.Tags.Count;
            int intersection = matchingTags.Count;
            int union = source.Tags.Count + cand.Tags.Count - intersection;
            double jaccardScore = (double)intersection / union;
            double similarity = (overlapScore * 0.6) + (jaccardScore * 0.4);
            // Boost range: rating 0→0.70×, rating 3→1.00×, rating 5→1.20×
            double ratingBoost = 1.0 + ((cand.Rating - 3.0) / 10.0);
            double finalScore = similarity * ratingBoost;

            recs.Add(new GameRecommendation
            {
                Game = cand,
                SimilarityScore = finalScore,
                MatchingTags = matchingTags,
                MatchReason = $"{matchingTags.Count}/{source.Tags.Count} tags match"
            });
        }

        Console.WriteLine($"  → {recs.Count} games passed strict criteria (4+ matching tags)");

        return recs
            .OrderByDescending(r => r.MatchingTags.Count)
            .ThenByDescending(r => r.SimilarityScore)
            .Take(max)
            .ToList();
    }
}