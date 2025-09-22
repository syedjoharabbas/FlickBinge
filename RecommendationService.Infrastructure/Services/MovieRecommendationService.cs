using RecommendationService.Core.Interfaces;
using RecommendationService.Core.Models;
using RecommendationService.Infrastructure.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polly;
using Microsoft.Extensions.Logging;
using Polly.Registry;

namespace RecommendationService.Infrastructure.Services
{
    public class MovieRecommendationService : IMovieRecommendationService
    {
        private readonly SemanticKernelConnector _kernelConnector;
        private readonly ILogger<MovieRecommendationService> _logger;
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;

        public MovieRecommendationService(SemanticKernelConnector kernelConnector, ILogger<MovieRecommendationService> logger, IReadOnlyPolicyRegistry<string> policyRegistry)
        {
            _kernelConnector = kernelConnector;
            _logger = logger;
            _policyRegistry = policyRegistry;
        }

        public async Task<MovieRecommendationResult> RecommendAsync(MovieRecommendationRequest request)
        {
            // Build a prompt for the model
            var prompt = BuildPrompt(request);

            // Execute using the registered policy from the PolicyRegistry
            if (!_policyRegistry.TryGet<IAsyncPolicy<string>>("RecommendationPolicy", out var policy))
            {
                _logger.LogWarning("RecommendationPolicy not found in registry; calling connector directly");
                try
                {
                    var directResponse = await _kernelConnector.GetRecommendationsAsync(prompt);
                    return new MovieRecommendationResult { RecommendedMovies = directResponse.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(m => m.Trim()).ToList() };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Recommendation] Direct recommendation call failed");
                    return new MovieRecommendationResult { RecommendedMovies = new List<string>() };
                }
            }

            string response;
            try
            {
                response = await policy.ExecuteAsync(ct => _kernelConnector.GetRecommendationsAsync(prompt), CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Recommendation] Recommendations failed after policy execution");
                return new MovieRecommendationResult { RecommendedMovies = new List<string>() };
            }

            // Simple parsing: assume comma-separated movie titles
            var movies = response.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(m => m.Trim())
                                 .ToList();

            return new MovieRecommendationResult
            {
                RecommendedMovies = movies
            };
        }

        private string BuildPrompt(MovieRecommendationRequest request)
        {
            var watched = string.Join(", ", request.WatchedMovies);
            var interests = string.Join(", ", request.Interests);

            return  $@"You are a helpful movie recommendation assistant. Given the movies a user has already watched: {watched} and their interests: {interests} Provide 5 movie recommendations that match their taste. List them separated by commas.";
        }
    }
}