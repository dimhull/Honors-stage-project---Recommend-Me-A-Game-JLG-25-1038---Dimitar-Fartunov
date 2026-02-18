using BlazorApp1.Components.Models;
using BlazorApp1.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp1.Components.Services
{
    public class WishlistService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly AuthService _authService;

        public event Action? OnWishlistChanged;

        public WishlistService(IDbContextFactory<ApplicationDbContext> contextFactory, AuthService authService)
        {
            _contextFactory = contextFactory;
            _authService = authService;
        }

        public async Task<bool> AddToWishlistAsync(Game game)
        {
            if (_authService.CurrentUser == null)
                return false;

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var existingItem = await context.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == _authService.CurrentUser.Id && w.GameId == game.Id);

                if (existingItem != null)
                {
                    Console.WriteLine("Game already in wishlist");
                    return false;
                }

                var wishlistItem = new WishlistItem
                {
                    UserId = _authService.CurrentUser.Id,
                    GameId = game.Id,
                    GameName = game.Name,
                    GameImage = game.BackgroundImage,
                    GameRating = game.Rating,
                    GameReleased = game.Released,
                    AddedAt = DateTime.UtcNow
                };

                context.WishlistItems.Add(wishlistItem);
                await context.SaveChangesAsync();

                OnWishlistChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding to wishlist: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveFromWishlistAsync(int gameId)
        {
            if (_authService.CurrentUser == null)
                return false;

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var item = await context.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == _authService.CurrentUser.Id && w.GameId == gameId);

                if (item == null)
                    return false;

                context.WishlistItems.Remove(item);
                await context.SaveChangesAsync();

                OnWishlistChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing from wishlist: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsInWishlistAsync(int gameId)
        {
            if (_authService.CurrentUser == null)
                return false;

            try
            {
                using var context = _contextFactory.CreateDbContext();

                return await context.WishlistItems
                    .AnyAsync(w => w.UserId == _authService.CurrentUser.Id && w.GameId == gameId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking wishlist: {ex.Message}");
                return false;
            }
        }

        public async Task<List<WishlistItem>> GetUserWishlistAsync()
        {
            if (_authService.CurrentUser == null)
                return new List<WishlistItem>();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                return await context.WishlistItems
                    .Where(w => w.UserId == _authService.CurrentUser.Id)
                    .OrderByDescending(w => w.AddedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting wishlist: {ex.Message}");
                return new List<WishlistItem>();
            }
        }
    }
}