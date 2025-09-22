using MovieService.Core.Interfaces;
using MovieService.Infrastructure.Config;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;
using System.Net.Http;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register a PolicyRegistry using DI so policies can use the application's logging
builder.Services.AddSingleton<IReadOnlyPolicyRegistry<string>>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();

    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                var reason = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "unknown";
                logger.LogWarning(outcome.Exception, "[MovieService] Retry {RetryCount} after {Delay}s due to: {Reason}", retryCount, timespan.TotalSeconds, reason);
            });

    var circuitPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 3, durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, breakDelay, context) =>
            {
                var reason = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "unknown";
                logger.LogWarning(outcome.Exception, "[MovieService] Circuit opened for {BreakDelay}s due to: {Reason}", breakDelay.TotalSeconds, reason);
            },
            onReset: (context) => logger.LogInformation("[MovieService] Circuit reset"),
            onHalfOpen: () => logger.LogInformation("[MovieService] Circuit half-open: testing..."));

    var policyWrap = Policy.WrapAsync(retryPolicy, circuitPolicy);
    var registry = new PolicyRegistry { { "MovieHttpPolicy", policyWrap } };
    return (IReadOnlyPolicyRegistry<string>)registry;
});

// Configure HttpClient for IMovieService with named policy from registry
builder.Services.AddHttpClient<IMovieService, MovieService.Infrastructure.Services.MovieService>()
    .AddPolicyHandlerFromRegistry("MovieHttpPolicy");

var omdbKey = builder.Configuration["OMDb:ApiKey"];
if (string.IsNullOrEmpty(omdbKey))
{
    throw new InvalidOperationException("OMDb:ApiKey must be configured.");
}
var omdbKeyValue = omdbKey!; // local non-null copy for compiler
builder.Services.AddSingleton(new MovieServiceOptions { ApiKey = omdbKeyValue });

var app = builder.Build();

// Log important startup config with ILogger
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🔧 JWT/OMDb config: OMDbApiKey set={HasKey}", !string.IsNullOrEmpty(omdbKey));

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
