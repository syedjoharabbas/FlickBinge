using MovieService.Core.Interfaces;
using MovieService.Core.Models;
using MovieService.Infrastructure.Config;
using System.Net.Http.Json;

namespace MovieService.Infrastructure.Services
{
    public class MovieService : IMovieService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public MovieService(HttpClient httpClient, MovieServiceOptions options)
        {
            _httpClient = httpClient;
            _apiKey = options.ApiKey;
        }

        public async Task<List<Movie>> GetPopularMoviesAsync()
        {
            // Example: search for action movies
            var response = await _httpClient.GetFromJsonAsync<OmdbSearchResponse>(
                $"http://www.omdbapi.com/?s=action&type=movie&apikey={_apiKey}"
            );

            if (response?.Search == null)
                return new List<Movie>();

            return response.Search.Select(m => new Movie
            {
                Title = m.Title,
                Year = m.Year,
                Genre = m.Genre,
                Director = m.Director,
                Actors = m.Actors,
                Plot = m.Plot,
                Poster = m.Poster,
                imdbID = m.imdbID
            }).ToList();
        }

        private class OmdbSearchResponse
        {
            public List<OmdbMovieDto>? Search { get; set; }
            public string? totalResults { get; set; }
            public string? Response { get; set; }
        }

        private class OmdbMovieDto
        {
            public string Title { get; set; } = string.Empty;
            public string Year { get; set; } = string.Empty;
            public string Genre { get; set; } = string.Empty;
            public string Director { get; set; } = string.Empty;
            public string Actors { get; set; } = string.Empty;
            public string Plot { get; set; } = string.Empty;
            public string Poster { get; set; } = string.Empty;
            public string imdbID { get; set; } = string.Empty;
        }
    }
}
