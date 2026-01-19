using System.Text.RegularExpressions;
using BlazorApp1.Components.Models;

public static class ContentFilter
{
    private static readonly HashSet<int> NsfwTagIds = new HashSet<int>
    {
    };

    private static readonly HashSet<string> NsfwKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "nsfw", "hentai", "erotic", "sexual content", "nudity", "porn", "xxx"
};

    public static List<Game> FilterAndClean(List<Game> games)
    {
        if (games == null) return new List<Game>();

        var safeGames = games.Where(g => !IsNsfw(g)).ToList();

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
                .Where(tag => !string.IsNullOrWhiteSpace(tag.Name) && IsEnglish(tag.Name))
                .ToList();
        }
    }
    public static bool IsEnglish(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Updated Regex with colon support
        var regex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9\s\-\.\'\:\u00C0-\u00FF]+$");
        return regex.IsMatch(text);
    }
}