using MovieService.Models;

namespace MovieService.Services
{
    public interface IMovieService
    {
        Task<List<Movie>> GetPopularMoviesAsync();
    }
}