using BlazorApp1.Components;
using BlazorApp1.Components.Models;
using BlazorApp1.Components.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure RAWG API settings
builder.Services.Configure<RawgApiSettings>(
    builder.Configuration.GetSection("RawgApi"));

// Register HTTP client and services
builder.Services.AddHttpClient<RawgApiService>();
builder.Services.AddScoped<GameRecommendationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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