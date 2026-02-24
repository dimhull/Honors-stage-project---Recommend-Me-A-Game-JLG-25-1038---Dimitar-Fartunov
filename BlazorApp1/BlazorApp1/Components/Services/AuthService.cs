using BlazorApp1.Components.Models;
using BlazorApp1.Data;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BlazorApp1.Components.Services
{
    public class AuthService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private User? _currentUser;
        private bool _initialized = false;

        public event Action? OnAuthStateChanged;

        private readonly ProtectedSessionStorage _sessionStorage;

        public AuthService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ProtectedSessionStorage sessionStorage)
        {
            _contextFactory = contextFactory;
            _sessionStorage = sessionStorage;
        }

        public User? CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;

        public async Task<(bool Success, string Message)> RegisterAsync(string email, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "All fields are required");
            }

            if (password.Length < 6)
            {
                return (false, "Password must be at least 6 characters");
            }

            using var context = _contextFactory.CreateDbContext();

            var existingUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email == email || u.Username == username);

            if (existingUser != null)
            {
                return (false, "Email or username already exists");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Email = email,
                Username = username,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            _currentUser = user;
            await _sessionStorage.SetAsync("userId", user.Id);
            OnAuthStateChanged?.Invoke();

            return (true, "Registration successful!");
        }

        public async Task<(bool Success, string Message)> LoginAsync(string emailOrUsername, string password)
        {
            using var context = _contextFactory.CreateDbContext();

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Email == emailOrUsername || u.Username == emailOrUsername);

            if (user == null)
            {
                return (false, "Invalid credentials");
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return (false, "Invalid credentials");
            }

            _currentUser = user;
            await _sessionStorage.SetAsync("userId", user.Id);
            OnAuthStateChanged?.Invoke();

            return (true, "Login successful!");
        }

        public async Task Logout()
        {
            _currentUser = null;
            await _sessionStorage.DeleteAsync("userId");
            OnAuthStateChanged?.Invoke();
        }

        public async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                var result = await _sessionStorage.GetAsync<int>("userId");

                if (result.Success && result.Value > 0)
                {
                    using var context = _contextFactory.CreateDbContext();
                    _currentUser = await context.Users.FindAsync(result.Value);

                    if (_currentUser != null)
                        OnAuthStateChanged?.Invoke();
                }
            }
            catch
            {
                // Session storage unavailable
            }
        }
    }
}