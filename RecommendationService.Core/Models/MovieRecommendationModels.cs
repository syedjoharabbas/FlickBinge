using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecommendationService.Core.Models
{
    public class MovieRecommendationRequest
    {
        public List<string> WatchedMovies { get; set; } = new();
        public List<string> Interests { get; set; } = new();
    }

    public class MovieRecommendationResult
    {
        public List<string> RecommendedMovies { get; set; } = new();
    }
}
