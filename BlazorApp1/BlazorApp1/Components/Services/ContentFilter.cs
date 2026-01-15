using BlazorApp1.Components.Models;
    public static class ContentFilter
    {
        private static readonly HashSet<int> NsfwTagIds = new HashSet<int>
        {
            42,   // NSFW
            96,   // Sexual Content
            97,   // Nudity
            222,  // Hentai
            398,  // Adult
            1070, // Erotic
        };

        private static readonly HashSet<string> NsfwKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "nsfw",
            "hentai",
            "erotic",
            "adult only",
            "sexual content",
            "nudity",
            "porn",
            "xxx",
            "18+",
            "mature content"
        };

        public static bool IsNsfw(Game game)
        {
            if (game == null) return false;

            // Check tags
            if (game.Tags != null && game.Tags.Any(tag =>
                NsfwTagIds.Contains(tag.Id) ||
                NsfwKeywords.Any(keyword => tag.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))))
            {
                return true;
            }

            // Check game name
            if (!string.IsNullOrEmpty(game.Name) &&
                NsfwKeywords.Any(keyword => game.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Check genres
            if (game.Genres != null && game.Genres.Any(genre =>
                NsfwKeywords.Any(keyword => genre.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))))
            {
                return true;
            }

            return false;
        }

        public static List<Game> FilterNsfw(List<Game> games)
        {
            return games.Where(g => !IsNsfw(g)).ToList();
        }
    }