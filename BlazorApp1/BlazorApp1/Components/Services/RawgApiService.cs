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
            // DON'T set BaseAddress - we'll use full URLs

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<Game>> SearchGamesAsync(string query, int pageSize = 10)
        {
            try
            {
                // Use FULL URL with /api/ in the path
                var url = $"https://api.rawg.io/api/games?key={_settings.ApiKey}&search={Uri.EscapeDataString(query)}&page_size={pageSize}";

                Console.WriteLine($"Calling: {url}");

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Status: {response.StatusCode}");

                response.EnsureSuccessStatusCode();

                var result = JsonSerializer.Deserialize<RawgApiResponse<Game>>(content, _jsonOptions);

                Console.WriteLine($"Games found: {result?.Results?.Count ?? 0}");

                return result?.Results ?? new List<Game>();
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

                Console.WriteLine($"Calling: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var game = JsonSerializer.Deserialize<Game>(content, _jsonOptions);

                return game;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting game: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Game>> GetGamesByTagsAsync(List<int> tagIds, int pageSize = 20)
        {
            try
            {
                var tagsParam = string.Join(",", tagIds);
                var url = $"https://api.rawg.io/api/games?key={_settings.ApiKey}&tags={tagsParam}&page_size={pageSize}";

                Console.WriteLine($"Calling: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RawgApiResponse<Game>>(content, _jsonOptions);

                return result?.Results ?? new List<Game>();
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
                var url = $"https://api.rawg.io/api/games?key={_settings.ApiKey}&ordering=-rating&page_size={pageSize}";

                Console.WriteLine($"Calling: {url}");

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Status: {response.StatusCode}");

                response.EnsureSuccessStatusCode();

                var result = JsonSerializer.Deserialize<RawgApiResponse<Game>>(content, _jsonOptions);

                Console.WriteLine($"Popular games found: {result?.Results?.Count ?? 0}");

                return result?.Results ?? new List<Game>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting popular games: {ex.Message}");
                return new List<Game>();
            }
        }
    }
}