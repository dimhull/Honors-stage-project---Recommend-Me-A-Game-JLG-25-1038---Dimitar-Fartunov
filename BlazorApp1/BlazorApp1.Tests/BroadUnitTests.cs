using Xunit;
using BlazorApp1.Components.Models;
using BlazorApp1.Components.Service;
using System.Net;
using Microsoft.Extensions.Options; 
using Moq; 
using Moq.Protected;
using Microsoft.Extensions.Caching.Memory;


public class Tests
{
    [Fact]
    public void IsEnglish_ShouldAllowStandardEnglishAndAccents()
    {
        // Arrange & Act
        var result1 = ContentFilter.IsEnglish("Souls-like");
        var result2 = ContentFilter.IsEnglish("Pokémon");
        var result3 = ContentFilter.IsEnglish("Dark Souls III: The Ringed City");

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
    }

    [Fact]
    public void IsEnglish_ShouldRejectNonLatinScripts()
    {
        // Arrange & Act
        var result1 = ContentFilter.IsEnglish("Дарк Соулс"); 
        var result2 = ContentFilter.IsEnglish("ダークソウル"); 

        // Assert
        Assert.False(result1);
        Assert.False(result2);
    }

    [Fact]
    public void IsNsfw_ShouldBlockNSFWButAllowMatureGames()
    {
        // Arrange
        var darkSouls = new Game { Name = "Dark Souls", Tags = new List<Tag> { new Tag { Name = "Difficult" } } };
        var pGame = new Game { Name = "Hentai Adventure", Tags = new List<Tag> { new Tag { Name = "NSFW" } } };

        // Act
        var isDsNsfw = ContentFilter.IsNsfw(darkSouls);
        var isPNsfw = ContentFilter.IsNsfw(pGame);

        // Assert
        Assert.False(isDsNsfw); // Should NOT be blocked
        Assert.True(isPNsfw);  // Should be blocked
    }

    [Fact]
    public async Task GetPopularGamesAsync_ReturnsCleanedGames()
    {
        // This test works because it uses the REAL service, but a MOCKED network handler
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"results\": [{\"id\": 1, \"name\": \"Dark Souls\", \"tags\": [{\"name\": \"Difficult\"}]}]}")
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHandler.Object);
        var settings = Options.Create(new RawgApiSettings { ApiKey = "test-key" });

        var service = new RawgApiService(httpClient, settings);

        var result = await service.GetPopularGamesAsync();

        Assert.Single(result);
        Assert.Equal("Dark Souls", result[0].Name);
    }

    [Fact]
    public void ProcessRecommendations_ShouldReturnGamesWithFourPlusMatchingTags()
    {
        // --- ARRANGE ---
        var httpClient = new HttpClient();
        var settings = Options.Create(new RawgApiSettings { ApiKey = "test" });

        // 1. Mock the class directly (requires methods to be virtual)
        var mockApi = new Mock<RawgApiService>(httpClient, settings);
        var mockCache = new Mock<IMemoryCache>();

        // 2. Initialize the service
        var _recommendationService = new GameRecommendationService(mockApi.Object, mockCache.Object);

        var sourceGame = new Game
        {
            Id = 1,
            Name = "Source",
            Tags = new List<Tag> {
            new Tag { Id = 10, Name = "RPG" },
            new Tag { Id = 11, Name = "Fantasy" },
            new Tag { Id = 12, Name = "Difficult" },
            new Tag { Id = 13, Name = "Atmospheric" }
        }
        };

        var candidatePool = new List<Game> {
        new Game { Id = 2, Name = "Strong Match", Tags = sourceGame.Tags },
        new Game { Id = 3, Name = "Weak Match", Tags = new List<Tag> { new Tag { Id = 10, Name = "RPG" } } }
    };

        // --- ACT ---
        var results = _recommendationService.ProcessRecommendations(sourceGame, candidatePool, max: 5, minMatchingTags: 4);

        // --- ASSERT ---
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("Strong Match", results[0].Game.Name);
    }

    [Fact]
    public void ProcessRecommendations_ShouldExcludeGames_WhenRequiredTagNotPresent()
    {
        // --- ARRANGE ---
        var httpClient = new HttpClient();
        var settings = Options.Create(new RawgApiSettings { ApiKey = "test" });
        var mockApi = new Mock<RawgApiService>(httpClient, settings);
        var mockCache = new Mock<IMemoryCache>();
        var service = new GameRecommendationService(mockApi.Object, mockCache.Object);

        var sourceTags = new List<Tag> {
            new Tag { Id = 10, Name = "RPG" },
            new Tag { Id = 11, Name = "Fantasy" },
            new Tag { Id = 12, Name = "Difficult" },
            new Tag { Id = 13, Name = "Atmospheric" }
        };

        var source = new Game { Id = 1, Name = "Source", Tags = sourceTags };

        // hasRequired includes all source tags PLUS the required tag (Id=99)
        var hasRequired = new Game {
            Id = 2, Name = "Has Required Tag",
            Tags = new List<Tag>(sourceTags) { new Tag { Id = 99, Name = "Open World" } }
        };

        // missingRequired has the same overlap but lacks tag Id=99
        var missingRequired = new Game { Id = 3, Name = "Missing Required Tag", Tags = sourceTags };

        var requiredTagIds = new HashSet<int> { 99 };

        // --- ACT ---
        var results = service.ProcessRecommendations(source, new List<Game> { hasRequired, missingRequired },
            max: 5, minMatchingTags: 4, requiredTagIds);

        // --- ASSERT ---
        Assert.Single(results);
        Assert.Equal("Has Required Tag", results[0].Game.Name);
    }

    [Fact]
    public void ProcessRecommendations_ShouldApplyRatingBoost_Correctly()
    {
        // --- ARRANGE ---
        var httpClient = new HttpClient();
        var settings = Options.Create(new RawgApiSettings { ApiKey = "test" });
        var mockApi = new Mock<RawgApiService>(httpClient, settings);
        var mockCache = new Mock<IMemoryCache>();
        var service = new GameRecommendationService(mockApi.Object, mockCache.Object);

        var sourceTags = new List<Tag> {
            new Tag { Id = 1, Name = "RPG" },
            new Tag { Id = 2, Name = "Action" },
            new Tag { Id = 3, Name = "Fantasy" },
            new Tag { Id = 4, Name = "Atmospheric" }
        };
        var source = new Game { Id = 1, Tags = sourceTags };

        // Both candidates have identical tag overlap — only rating differs
        var highRated = new Game { Id = 2, Name = "High Rated", Rating = 5.0, Tags = sourceTags };
        var lowRated  = new Game { Id = 3, Name = "Low Rated",  Rating = 1.0, Tags = sourceTags };

        // --- ACT ---
        var results = service.ProcessRecommendations(source, new List<Game> { highRated, lowRated },
            max: 5, minMatchingTags: 4);

        // --- ASSERT ---
        // boost(5.0) = 1.0 + (5.0-3.0)/10.0 = 1.2
        // boost(1.0) = 1.0 + (1.0-3.0)/10.0 = 0.8
        // ratio = 1.2 / 0.8 = 1.5 exactly
        Assert.Equal(2, results.Count);
        var highScore = results.First(r => r.Game.Name == "High Rated").SimilarityScore;
        var lowScore  = results.First(r => r.Game.Name == "Low Rated").SimilarityScore;

        Assert.True(highScore > lowScore);
        Assert.Equal(1.5, highScore / lowScore, precision: 5);
    }

    [Fact]
    public void FilterAndClean_ShouldRemoveNsfwAndNonEnglish_KeepValid()
    {
        // --- ARRANGE ---
        var games = new List<Game>
        {
            new Game
            {
                Id = 1,
                Name = "Valid Game",
                Tags = new List<Tag>
                {
                    new Tag { Name = "RPG" },
                    new Tag { Name = "ダークソウル" },  // non-English — must be removed
                    new Tag { Name = "singleplayer" }, // useless  — must be removed
                }
            },
            new Game
            {
                Id = 2,
                Name = "Hentai Adventure",
                Tags = new List<Tag> { new Tag { Name = "nsfw" } }
            }
        };

        // --- ACT ---
        var result = ContentFilter.FilterAndClean(games);

        // --- ASSERT ---
        Assert.Single(result);                           // NSFW game removed
        Assert.Equal("Valid Game", result[0].Name);
        Assert.Single(result[0].Tags);                   // only "RPG" survives cleaning
        Assert.Equal("RPG", result[0].Tags[0].Name);
    }


}

