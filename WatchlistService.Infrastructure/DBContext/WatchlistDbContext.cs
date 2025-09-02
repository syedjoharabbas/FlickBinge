using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatchlistService.Core.Models;

namespace WatchlistService.Infrastructure.DBContext
{
    public class WatchlistDbContext : DbContext
    {
        public WatchlistDbContext(DbContextOptions<WatchlistDbContext> options) : base(options) { }

        public DbSet<Watchlist> Watchlists => Set<Watchlist>();
        public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Watchlist>()
                .HasMany(w => w.Items)
                .WithOne()
                .HasForeignKey(i => i.WatchlistId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
