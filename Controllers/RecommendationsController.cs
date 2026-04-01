using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using прпгр.Services;

namespace прпгр.Controllers
{
    [Authorize]
    public class RecommendationsController : Controller
    {
        private readonly RecommendationService _recommendationService;
        private readonly MaterialService _materialStore;

        public RecommendationsController(
            RecommendationService recommendationService,
            MaterialService materialStore)
        {
            _recommendationService = recommendationService;
            _materialStore = materialStore;
        }

        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var recommendations = _recommendationService.GetRecommendations(userId, 10);

            var model = recommendations.Select(m => new MaterialViewModel
            {
                Id = m.Id,
                Title = m.Title,
                Subject = m.Subject,
                Course = m.Course,
                Topic = m.Topic,
                ShortDescription = !string.IsNullOrEmpty(m.Description) && m.Description.Length > 150
                    ? m.Description.Substring(0, 150) + "..."
                    : m.Description,
                IsPremium = m.IsPremium,
                Status = m.Status,
                AverageRating = m.AverageRating,
                RatingsCount = m.RatingsCount,
                Tags = _materialStore.GetTagsForMaterial(m.Id).Select(t => t.Name).ToList()
            }).ToList();

            return View(model);
        }
    }
}
