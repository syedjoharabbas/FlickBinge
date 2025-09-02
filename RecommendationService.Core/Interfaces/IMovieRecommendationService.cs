using RecommendationService.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecommendationService.Core.Interfaces
{
    public interface IMovieRecommendationService
    {
        Task<MovieRecommendationResult> RecommendAsync(MovieRecommendationRequest request);
    }
}
