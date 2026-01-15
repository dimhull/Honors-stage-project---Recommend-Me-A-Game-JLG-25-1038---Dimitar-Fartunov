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

            Console.WriteLine($"\n=== FINDING RECOMMENDATIONS FOR: {sourceGame.Name} ===");
            Console.WriteLine($"Source has {sourceGame.Tags.Count} tags: {string.Join(", ", sourceGame.Tags.Take(5).Select(t => t.Name))}");

            var sourceTagIds = sourceGame.Tags.Select(t => t.Id).ToList();


            var candidates = await FetchLargeCandidatePool(sourceTagIds);

            var safeCandidates = ContentFilter.FilterNsfw(candidates);
            var filteredCount = candidates.Count - safeCandidates.Count;

            if (filteredCount > 0)
            {
                Console.WriteLine($"🔒 Filtered out {filteredCount} NSFW games");
            }

            Console.WriteLine($"→ Processing {safeCandidates.Count} candidate games...");

            // Process with STRICT rules (4+ matching tags)
            var recommendations = ProcessRecommendations(sourceGame, safeCandidates, maxResults, minMatchingTags: 4);

            Console.WriteLine($"✓ Found {recommendations.Count} high-quality recommendations\n");

            // Cache for 24 hours
            _cache.Set(cacheKey, recommendations, TimeSpan.FromHours(24));

            return recommendations;
        }

        private async Task<List<Game>> FetchLargeCandidatePool(List<int> tagIds)
        {
            const int TOTAL_PAGES = 25; // 1000 games
            const int PAGE_SIZE = 40;

            Console.WriteLine($"Fetching {TOTAL_PAGES} pages ({TOTAL_PAGES * PAGE_SIZE} games total)...");

            var allCandidates = new List<Game>();

            // Fetch in batches of 5 pages at a time
            for (int batchStart = 1; batchStart <= TOTAL_PAGES; batchStart += 5)
            {
                var batchEnd = Math.Min(batchStart + 4, TOTAL_PAGES);
                var batchTasks = new List<Task<List<Game>>>();

                for (int page = batchStart; page <= batchEnd; page++)
                {
                    batchTasks.Add(_apiService.GetGamesByTagsAsync(tagIds, PAGE_SIZE, page));
                }

                var batchResults = await Task.WhenAll(batchTasks);
                var batchGames = batchResults.SelectMany(g => g).ToList();

                allCandidates.AddRange(batchGames);

                Console.WriteLine($"  Fetched pages {batchStart}-{batchEnd}: {batchGames.Count} games (Total: {allCandidates.Count})");

                if (batchEnd < TOTAL_PAGES)
                {
                    await Task.Delay(200);
                }
            }

            var uniqueCandidates = allCandidates.DistinctBy(g => g.Id).ToList();
            Console.WriteLine($"✓ Total unique games fetched: {uniqueCandidates.Count}");

            return uniqueCandidates;
        }

        private List<GameRecommendation> ProcessRecommendations(Game source, List<Game> candidates, int max, int minMatchingTags)
        {
            var recs = new List<GameRecommendation>();

            foreach (var cand in candidates)
            {
                if (cand.Id == source.Id) continue;
                if (cand.Tags == null || !cand.Tags.Any()) continue;

                var matchingTags = cand.Tags
                    .Where(t => source.Tags.Any(st => st.Id == t.Id))
                    .ToList();

                if (matchingTags.Count < minMatchingTags) continue;

                double overlapScore = (double)matchingTags.Count / source.Tags.Count;
                int intersection = matchingTags.Count;
                int union = source.Tags.Count + cand.Tags.Count - intersection;
                double jaccardScore = (double)intersection / union;
                double similarity = (overlapScore * 0.6) + (jaccardScore * 0.4);
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

            Console.WriteLine($"  → {recs.Count} games passed strict criteria (4+ matching tags)");

            return recs
                .OrderByDescending(r => r.MatchingTags.Count)
                .ThenByDescending(r => r.SimilarityScore)
                .Take(max)
                .ToList();
        }
    }
