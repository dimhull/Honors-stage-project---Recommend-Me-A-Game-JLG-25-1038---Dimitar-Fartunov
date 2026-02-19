using BlazorApp1.Components;
using BlazorApp1.Components.Models;
using BlazorApp1.Components.Service;
using BlazorApp1.Components.Services;
using BlazorApp1.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure RAWG API settings
builder.Services.Configure<RawgApiSettings>(
    builder.Configuration.GetSection("RawgApi"));

// Add Memory Cache
builder.Services.AddMemoryCache();

var mysqlPassword = Environment.GetEnvironmentVariable("PLACEHOLDER");

if (string.IsNullOrEmpty(mysqlPassword))
{
    throw new InvalidOperationException(
        "MYSQL_PASSWORD environment variable is not set. " +
        "Please set it using: setx MYSQL_PASSWORD \"your_password\" and restart the application.");
}

var connectionString = $"server=localhost;port=3306;database=gamerecdb;user=root;password={mysqlPassword};SslMode=none;";

// Add MySQL Database
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(9, 6, 0))));

// Register services
builder.Services.AddHttpClient<RawgApiService>();
builder.Services.AddScoped<GameRecommendationService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddScoped<WishlistService>();

var app = builder.Build();

/// Verify database connection on startup
using (var scope = app.Services.CreateScope())
{
    // Use the factory to create a context
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    using var db = dbFactory.CreateDbContext();

    try
    {
        Console.WriteLine("\n=== DATABASE CONNECTION TEST ===");

        var canConnect = db.Database.CanConnect();

        if (canConnect)
        {
            Console.WriteLine(" DATABASE CONNECTION SUCCESSFUL!");
            Console.WriteLine($"Connected to database: {db.Database.GetDbConnection().Database}");
        }
        else
        {
            Console.WriteLine(" DATABASE CONNECTION FAILED!");
            throw new Exception("Unable to connect to database");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ DATABASE ERROR: {ex.Message}");

        if (ex.InnerException != null)
        {
            Console.WriteLine($"Details: {ex.InnerException.Message}");
        }

        throw;
    }

    Console.WriteLine("================================\n");
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();