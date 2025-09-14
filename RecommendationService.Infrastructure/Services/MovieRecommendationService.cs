using RecommendationService.Core.Interfaces;
using RecommendationService.Core.Models;
using RecommendationService.Infrastructure.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecommendationService.Infrastructure.Services
{
    public class MovieRecommendationService : IMovieRecommendationService
    {
        private readonly SemanticKernelConnector _kernelConnector;

        public MovieRecommendationService(SemanticKernelConnector kernelConnector)
        {
            _kernelConnector = kernelConnector;
        }

        public async Task<MovieRecommendationResult> RecommendAsync(MovieRecommendationRequest request)
        {
            // Build a prompt for the model
            var prompt = BuildPrompt(request);

            // Use SK to get recommendations
            var response = await _kernelConnector.GetRecommendationsAsync(prompt);

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