using MovieService.Core.Models;

namespace MovieService.Core.Interfaces
{
    public interface IMovieService
    {
        Task<List<Movie>> GetPopularMoviesAsync();
    }
}