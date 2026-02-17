using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Components.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<WishlistItem> WishlistItems { get; set; } = new();
    }
}