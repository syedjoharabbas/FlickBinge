using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatchlistService.Core.Models;

namespace WatchlistService.Core.Interfaces
{
    public interface IWatchlistService
    {
        Task CreateWatchlistAsync(Guid userId);
        Task AddMovieAsync(Guid userId, string movieId);
        Task RemoveMovieAsync(Guid userId, string movieId);
        Task<List<string>> GetWatchlistAsync(Guid userId);
    }
}
