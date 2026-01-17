using System.Text.Json;
using BlazorApp1.Components.Models;
using Microsoft.Extensions.Options;

namespace BlazorApp1.Components.Service
{
    public class RawgApiService
    {
        private readonly HttpClient _httpClient;
        private readonly RawgApiSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions;

        public RawgApiService(HttpClient httpClient, IOptions<RawgApiSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<List<Game>> SearchGamesAsync(string query, int pageSize = 10)
        {
            try
            {
                var url = $"https://api.rawg.io/api/games?key={_settings.ApiKey}&search={Uri.EscapeDataString(query)}&page_size={pageSize}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RawgApiResponse<Game>>(content, _jsonOptions);

                // Filter out NSFW and clean tags to English only
                return ContentFilter.FilterAndClean(result?.Results ?? new List<Game>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching games: {ex.Message}");
                return new List<Game>();
            }
        }

        public async Task<Game?> GetGameByIdAsync(int gameId)
        {
            try
            {
                var url = $"https://api.rawg.io/api/games/{gameId}?key={_settings.ApiKey}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var game = JsonSerializer.Deserialize<Game>(content, _jsonOptions);

                // Clean tags for the single game and check NSFW status
                if (game != null)
                {
                    if (ContentFilter.IsNsfw(game)) return null;
                    ContentFilter.CleanSingleGameTags(game); // Ensure English only
                }

                return game;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting game: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Game>> GetGamesByTagsAsync(List<int> tagIds, int pageSize = 40, int page = 1)
        {
            try
            {
                var tags = string.Join(",", tagIds);
                var url = $"https://api.rawg.io/api/games?tags={tags}&page_size={pageSize}&page={page}&key={_settings.ApiKey}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RawgApiResponse<Game>>(content, _jsonOptions);

                return ContentFilter.FilterAndClean(result?.Results ?? new List<Game>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting games by tags: {ex.Message}");
                return new List<Game>();
            }
        }

        public async Task<List<Game>> GetPopularGamesAsync(int pageSize = 12)
        {
            try
            {
                var end = DateTime.Now.ToString("yyyy-MM-dd");
                var start = DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd");

                var url = $"https://api.rawg.io/api/games?key={_settings.ApiKey}&ordering=-added&dates={start},{end}&page_size={pageSize}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RawgApiResponse<Game>>(content, _jsonOptions);

                return ContentFilter.FilterAndClean(result?.Results ?? new List<Game>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting popular games: {ex.Message}");
                return new List<Game>();
            }
        }
    }
}