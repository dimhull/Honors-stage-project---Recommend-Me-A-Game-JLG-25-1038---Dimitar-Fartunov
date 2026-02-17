using BlazorApp1.Components.Models;
using BlazorApp1.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp1.Components.Services
{
    public class WishlistService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;

        public event Action? OnWishlistChanged;

        public WishlistService(ApplicationDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<bool> AddToWishlistAsync(Game game)
        {
            if (_authService.CurrentUser == null)
                return false;

            try
            {
                var existingItem = await _context.WishlistItems
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

                _context.WishlistItems.Add(wishlistItem);
                await _context.SaveChangesAsync();

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
                var item = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == _authService.CurrentUser.Id && w.GameId == gameId);

                if (item == null)
                    return false;

                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();

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

            return await _context.WishlistItems
                .AnyAsync(w => w.UserId == _authService.CurrentUser.Id && w.GameId == gameId);
        }

        public async Task<List<WishlistItem>> GetUserWishlistAsync()
        {
            if (_authService.CurrentUser == null)
                return new List<WishlistItem>();

            return await _context.WishlistItems
                .Where(w => w.UserId == _authService.CurrentUser.Id)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();
        }
    }
}