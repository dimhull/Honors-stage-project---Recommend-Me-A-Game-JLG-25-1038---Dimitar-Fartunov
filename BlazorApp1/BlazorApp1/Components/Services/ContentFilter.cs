using System.Text.RegularExpressions;
using BlazorApp1.Components.Models;

public static class ContentFilter
{
    private static readonly HashSet<int> NsfwTagIds = new HashSet<int>
    {
    };

    private static readonly HashSet<string> NsfwKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "nsfw", "hentai", "erotic", "nudity", "porn", "xxx"
    };

    private static readonly HashSet<string> UselessKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "singleplayer",
        "multiplayer",
        "co-op",
        "online co-op",
        "local co-op",
        "steam achievements",
        "steam cloud",
        "full controller support",
        "partial controller support",
        "steam-trading-cards",
        "in-app purchases",
        "cross-platform multiplayer",
        "stats",
        "captions available",
        "commentary available",
        "level editor",
        "modding",
        "workshop",
        "achievements",
        "steam achievements",
        "leaderboards",
        "cloud saves"
    };

    public static List<Game> FilterAndClean(List<Game> games)
    {
        if (games == null) return new List<Game>();

        // Filter out NSFW games
        var safeGames = games.Where(g => !IsNsfw(g)).ToList();

        // Clean tags for all safe games
        foreach (var game in safeGames)
        {
            CleanSingleGameTags(game);
        }

        return safeGames;
    }

    public static bool IsNsfw(Game game)
    {
        if (game == null) return false;

        if (game.Tags != null && game.Tags.Any(tag =>
            NsfwTagIds.Contains(tag.Id) ||
            NsfwKeywords.Any(keyword => tag.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(game.Name) &&
            NsfwKeywords.Any(keyword => game.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (game.Genres != null && game.Genres.Any(genre =>
            NsfwKeywords.Any(keyword => genre.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))))
        {
            return true;
        }

        return false;
    }

    public static void CleanSingleGameTags(Game game)
    {
        if (game?.Tags != null)
        {
            game.Tags = game.Tags
                .Where(tag =>
                    !string.IsNullOrWhiteSpace(tag.Name) &&
                    IsEnglish(tag.Name) &&
                    !IsUselessTag(tag.Name))
                .ToList();
        }
    }

    public static bool IsEnglish(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Updated Regex with colon support
        var regex = new Regex(@"^[a-zA-Z0-9\s\-\.\'\:\u00C0-\u00FF]+$");
        return regex.IsMatch(text);
    }

    public static bool IsUselessTag(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName)) return false;
        return UselessKeywords.Contains(tagName.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}