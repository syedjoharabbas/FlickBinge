using Microsoft.AspNetCore.Mvc;
using MovieService.Core.Interfaces;
using MovieService.Core.Models;

namespace FlickBinge.Movies.Controllers;

[ApiController]
[Route("[controller]")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MoviesController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet]
    public async Task<List<Movie>> Get()
    {
        return await _movieService.GetPopularMoviesAsync();
    }
}
