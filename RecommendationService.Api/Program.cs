using Microsoft.SemanticKernel;
using RecommendationService.Core.Interfaces;
using RecommendationService.Core.Models;
using RecommendationService.Infrastructure.SemanticKernel;
using RecommendationService.Infrastructure.Services;
using Polly;
using Polly.Registry;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();

// Register a PolicyRegistry for Recommendation semantic calls
builder.Services.AddSingleton<IReadOnlyPolicyRegistry<string>>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();

    var retryPolicy = Policy<string>
        .Handle<Exception>()
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), onRetry: (outcome, ts, retryCount, ctx) =>
        {
            var ex = outcome.Exception;
            logger.LogWarning(ex, "[Recommendation] Retry {RetryCount} after {Delay}s due to: {Reason}", retryCount, ts.TotalSeconds, ex?.Message ?? outcome.Result?.ToString());
        });

    var circuitPolicy = Policy<string>
        .Handle<Exception>()
        .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 3, durationOfBreak: TimeSpan.FromSeconds(30), onBreak: (outcome, breakDelay, ctx) =>
        {
            var ex = outcome.Exception;
            logger.LogWarning(ex, "[Recommendation] Circuit opened for {BreakDelay}s due to: {Reason}", breakDelay.TotalSeconds, ex?.Message ?? outcome.Result?.ToString());
        }, onReset: (ctx) => logger.LogInformation("[Recommendation] Circuit reset"), onHalfOpen: () => logger.LogInformation("[Recommendation] Circuit half-open: testing..."));

    var wrapped = Policy.WrapAsync(retryPolicy, circuitPolicy);
    var registry = new PolicyRegistry { { "RecommendationPolicy", wrapped } };
    return (IReadOnlyPolicyRegistry<string>)registry;
});

builder.Services.AddSingleton<SemanticKernelConnector>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["OpenAI:ApiKey"];
    if (string.IsNullOrEmpty(apiKey))
        throw new InvalidOperationException("OpenAI API key not set. Check environment variable OpenAI__ApiKey.");

    return new SemanticKernelConnector(apiKey);
});


builder.Services.AddScoped<IMovieRecommendationService, MovieRecommendationService>();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapPost("/", async (IMovieRecommendationService recommendationService, MovieRecommendationRequest request) =>
{
    var result = await recommendationService.RecommendAsync(request);
    return Results.Ok(result);
})
.WithName("GetMovieRecommendations")
.WithOpenApi();

app.Run();
