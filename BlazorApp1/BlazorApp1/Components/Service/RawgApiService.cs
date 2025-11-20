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
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // Search for games by name
        public async Task<List<Game>> SearchGamesAsync(string query, int pageSize = 10)
        {
            try
            {
                var url = $"/games?key={_settings.ApiKey}&search={Uri.EscapeDataString(query)}&page_size={pageSize}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RawgApiResponse<Game>>(content, _jsonOptions);

                return result?.Results ?? new List<Game>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching games: {ex.Message}");
                return new List<Game>();
            }
        }

        // Get a specific game by ID with full details
        public async Task<Game?> GetGameByIdAsync(int gameId)
        {
            try
            {
                var url = $"/games/{gameId}?key={_settings.ApiKey}";
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

        // Get games by multiple tag IDs
        public async Task<List<Game>> GetGamesByTagsAsync(List<int> tagIds, int pageSize = 20)
        {
            try
            {
                // RAWG accepts comma-separated tag IDs
                var tagsParam = string.Join(",", tagIds);
                var url = $"/games?key={_settings.ApiKey}&tags={tagsParam}&page_size={pageSize}";

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

        // Get popular/trending games for homepage
        public async Task<List<Game>> GetPopularGamesAsync(int pageSize = 12)
        {
            try
            {
                var url = $"/games?key={_settings.ApiKey}&ordering=-rating&page_size={pageSize}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RawgApiResponse<Game>>(content, _jsonOptions);

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