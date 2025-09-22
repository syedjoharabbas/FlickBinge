using Microsoft.EntityFrameworkCore;
using WatchlistService.Core.Interfaces;
using WatchlistService.Infrastructure.DBContext;
using WatchlistService.Infrastructure.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<WatchlistDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WatchlistConnection")));
builder.Services.AddScoped<IWatchlistService, WatchlistService.Infrastructure.Services.WatchlistService>();

builder.Services.AddSingleton<RabbitMQConsumer>();
builder.Services.AddHostedService<RabbitMQBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapPost("/{userId:guid}", async (Guid userId, IWatchlistService watchlistService) =>
{
    await watchlistService.CreateWatchlistAsync(userId);
    return Results.Created($"/{userId}", new { UserId = userId });
});

app.MapPost("/{userId:guid}/movies", async (Guid userId, string movieTitle, IWatchlistService watchlistService) =>
{
    await watchlistService.AddMovieAsync(userId, movieTitle);
    return Results.Ok(new { UserId = userId, Movie = movieTitle });
});

app.MapDelete("/{userId:guid}/movies/{movieTitle}", async (Guid userId, string movieTitle, IWatchlistService watchlistService) =>
{
    await watchlistService.RemoveMovieAsync(userId, movieTitle);
    return Results.NoContent();
});

app.MapGet("/{userId:guid}", async (Guid userId, IWatchlistService watchlistService) =>
{
    var movies = await watchlistService.GetWatchlistAsync(userId);
    return Results.Ok(movies);
});

app.Run();
