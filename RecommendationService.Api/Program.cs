using Microsoft.SemanticKernel;
using RecommendationService.Core.Interfaces;
using RecommendationService.Core.Models;
using RecommendationService.Infrastructure.SemanticKernel;
using RecommendationService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

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
