using Microsoft.EntityFrameworkCore;
using WatchlistService.Core.Interfaces;
using WatchlistService.Core.Models;
using WatchlistService.Infrastructure.DBContext;

namespace WatchlistService.Infrastructure.Services;

public class WatchlistService : IWatchlistService
{
    private readonly WatchlistDbContext _dbContext;

    public WatchlistService(WatchlistDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateWatchlistAsync(Guid userId)
    {
        var existing = await _dbContext.Watchlists.FirstOrDefaultAsync(w => w.UserId == userId);
        if (existing is null)
        {
            var watchlist = new Watchlist { UserId = userId };
            _dbContext.Watchlists.Add(watchlist);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task AddMovieAsync(Guid userId, string movieTitle)
    {
        var watchlist = await _dbContext.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (watchlist is null)
        {
            watchlist = new Watchlist { UserId = userId };
            _dbContext.Watchlists.Add(watchlist);
        }

        if (!watchlist.Items.Any(i => i.MovieTitle == movieTitle))
        {
            watchlist.Items.Add(new WatchlistItem { MovieTitle = movieTitle });
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveMovieAsync(Guid userId, string movieTitle)
    {
        var watchlist = await _dbContext.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (watchlist != null)
        {
            var item = watchlist.Items.FirstOrDefault(i => i.MovieTitle == movieTitle);
            if (item != null)
            {
                watchlist.Items.Remove(item);
                await _dbContext.SaveChangesAsync();
            }
        }
    }

    public async Task<List<string>> GetWatchlistAsync(Guid userId)
    {
        var watchlist = await _dbContext.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.UserId == userId);

        return watchlist?.Items.Select(i => i.MovieTitle).ToList() ?? new List<string>();
    }
}
