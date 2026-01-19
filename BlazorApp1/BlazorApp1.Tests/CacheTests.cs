using Xunit;
using Moq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using BlazorApp1.Components.Models;
using BlazorApp1.Components.Service;

public class RecommendationCacheTests
{
    private readonly Mock<RawgApiService> _mockApiService;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly GameRecommendationService _service;

    public RecommendationCacheTests()
    {
        // Arrange dependencies for the concrete RawgApiService class
        var httpClient = new HttpClient();
        var settings = Options.Create(new RawgApiSettings { ApiKey = "test-key" });

        // Mock the services
        _mockApiService = new Mock<RawgApiService>(httpClient, settings);
        _mockCache = new Mock<IMemoryCache>();

        // Initialize the service being tested
        _service = new GameRecommendationService(_mockApiService.Object, _mockCache.Object);
    }

    [Fact]
    public async Task GetRecommendations_ShouldReturnFromCache_WhenDataExists()
    {
        // --- ARRANGE ---
        int gameId = 123;

        var testGame = new Game
        {
            Id = gameId,
            Name = "Source Game",
            Tags = new List<Tag> { new Tag { Id = 1 }, new Tag { Id = 2 }, new Tag { Id = 3 }, new Tag { Id = 4 } }
        };

        string cacheKey = $"recs_{gameId}"; 

        var cachedData = new List<GameRecommendation>
    {
        new GameRecommendation { Game = new Game { Name = "Cached Game" } }
    };

        object outValue = cachedData;
        _mockCache.Setup(m => m.TryGetValue(cacheKey, out outValue)).Returns(true);

        // --- ACT ---
        var result = await _service.GetRecommendationsAsync(testGame);

        // --- ASSERT ---
        Assert.NotNull(result);
        Assert.NotEmpty(result); // This will fail if you get []
        Assert.Equal("Cached Game", result[0].Game.Name);
    }

    [Fact]
    public async Task GetRecommendations_ShouldCallApiAndSaveToCache_WhenCacheIsEmpty()
    {
        // --- ARRANGE ---
        int gameId = 456;
        var testGame = new Game
        {
            Id = gameId,
            Name = "Source Game",
            Tags = new List<Tag> {
            new Tag { Id = 1, Name = "RPG" },
            new Tag { Id = 2, Name = "Fantasy" },
            new Tag { Id = 3, Name = "Action" },
            new Tag { Id = 4, Name = "Adventure" }
        }
        };
        string cacheKey = $"recs_{gameId}";

        // 1. Mock Cache Miss
        object? outValue = null;
        _mockCache.Setup(m => m.TryGetValue(cacheKey, out outValue)).Returns(false);

        var mockCacheEntry = new Mock<ICacheEntry>();
        _mockCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);

        var candidatePool = new List<Game> {
        new Game {
            Id = 999,
            Name = "Valid Match",
            Tags = new List<Tag> {
                new Tag { Id = 1, Name = "RPG" },
                new Tag { Id = 2, Name = "Fantasy" },
                new Tag { Id = 3, Name = "Action" },
                new Tag { Id = 4, Name = "Adventure" }
            }
        }
    };

        _mockApiService
            .Setup(x => x.GetGamesByTagsAsync(It.IsAny<List<int>>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(candidatePool);

        // --- ACT ---
        await _service.GetRecommendationsAsync(testGame);

        // --- ASSERT ---
        _mockApiService.Verify(x => x.GetGamesByTagsAsync(It.IsAny<List<int>>(), It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce());
        _mockCache.Verify(m => m.CreateEntry(cacheKey), Times.Once);
    }
}