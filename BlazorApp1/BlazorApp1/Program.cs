using BlazorApp1.Components.Models;
using BlazorApp1.Components.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

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
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();