using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchlistService.Core.Models
{
    public class Watchlist
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }    // Link to user
        public List<WatchlistItem> Items { get; set; } = new();
    }

    public class WatchlistItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WatchlistId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
    }
}
