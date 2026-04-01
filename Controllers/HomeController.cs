using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using прпгр.Models;
using прпгр.Services;

namespace прпгр.Controllers
{
    public class HomeController : Controller
    {
        private readonly MaterialService _materialStore;
        private readonly UserService _userStore;
        private readonly RecommendationService _recommendationService;

        public HomeController(
            MaterialService materialStore,
            UserService userStore,
            RecommendationService recommendationService)
        {
            _materialStore = materialStore;
            _userStore = userStore;
            _recommendationService = recommendationService;
        }

        public IActionResult Index()
        {
            var allMaterials = _materialStore.GetAll();

            var totalMaterials = allMaterials.Count();
            var approvedMaterials = allMaterials.Count(m => m.Status == "Approved");

            var allTransactions = _userStore.GetAllTransactions();
            var premiumViewsCount = allTransactions.Count(t => t.Type == "ViewPremiumMaterial");

            int myMaterialsCount = 0;
            int myPremiumViewsCount = 0;
            int currentUserBalance = 0;
            var recommendedMaterials = new System.Collections.Generic.List<MaterialViewModel>();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    myMaterialsCount = _materialStore.GetByAuthor(userId).Count();

                    var myTransactions = allTransactions.Where(t => t.UserId == userId);
                    myPremiumViewsCount = myTransactions.Count(t => t.Type == "ViewPremiumMaterial");

                    var user = _userStore.FindById(userId);
                    currentUserBalance = user?.Balance ?? 0;

                    // Get recommendations
                    var recs = _recommendationService.GetRecommendations(userId, 5);
                    recommendedMaterials = recs.Select(m => new MaterialViewModel
                    {
                        Id = m.Id,
                        Title = m.Title,
                        Subject = m.Subject,
                        Course = m.Course,
                        Topic = m.Topic,
                        ShortDescription = !string.IsNullOrEmpty(m.Description) && m.Description.Length > 100
                            ? m.Description.Substring(0, 100) + "..."
                            : m.Description,
                        IsPremium = m.IsPremium,
                        Status = m.Status,
                        AverageRating = m.AverageRating,
                        RatingsCount = m.RatingsCount,
                        Tags = _materialStore.GetTagsForMaterial(m.Id).Select(t => t.Name).ToList()
                    }).ToList();
                }
            }

            var vm = new HomeDashboardViewModel
            {
                TotalMaterials = totalMaterials,
                ApprovedMaterials = approvedMaterials,
                PremiumViewsCount = premiumViewsCount,
                MyMaterialsCount = myMaterialsCount,
                MyPremiumViewsCount = myPremiumViewsCount,
                CurrentUserBalance = currentUserBalance,
                RecommendedMaterials = recommendedMaterials
            };

            return View(vm);
        }
    }
}
