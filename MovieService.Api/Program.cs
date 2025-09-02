using MovieService.Core.Interfaces;
using MovieService.Infrastructure.Config;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddHttpClient<IMovieService, MovieService.Infrastructure.Services.MovieService>();

var omdbKey = builder.Configuration["OMDb:ApiKey"];
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton(new MovieServiceOptions { ApiKey = omdbKey });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Minimal API endpoint to get popular movies
app.MapGet("/", async (IMovieService movieService) =>
{
    var movies = await movieService.GetPopularMoviesAsync();
    return Results.Ok(movies);
})
.WithName("GetPopularMovies")
.WithOpenApi();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
