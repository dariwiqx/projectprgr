using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using прпгр.Services;

namespace прпгр.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserService _userStore;
        private readonly MaterialService _materialStore;

        public ProfileController(UserService userStore, MaterialService materialStore)
        {
            _userStore = userStore;
            _materialStore = materialStore;
        }

        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = _userStore.FindById(userId);
            if (user == null) return NotFound();

            var materials = _materialStore.GetByAuthor(userId);
            var transactions = _userStore.GetTransactionsForUser(userId).ToList();
            var activities = _userStore.GetActivitiesForUser(userId);

            ViewBag.User = user;
            ViewBag.MaterialsCount = materials.Count;
            ViewBag.ApprovedCount = materials.Count(m => m.Status == "Approved");
            ViewBag.PendingCount = materials.Count(m => m.Status == "Pending");
            ViewBag.TotalDownloads = materials.Sum(m => m.DownloadCount);
            ViewBag.TotalRatings = materials.Sum(m => m.RatingsCount);
            ViewBag.AverageRating = materials.Where(m => m.RatingsCount > 0).Any()
                ? materials.Where(m => m.RatingsCount > 0).Average(m => m.AverageRating)
                : 0.0;
            ViewBag.TransactionsCount = transactions.Count;
            ViewBag.ActivitiesCount = activities.Count;

            return View();
        }
    }
}
