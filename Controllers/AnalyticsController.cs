using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

using прпгр.Services;

namespace прпгр.Controllers
{
    [Authorize]
    public class AnalyticsController : Controller
    {
        private readonly UserService _userStore;
        private readonly MaterialService _materialStore;

        public AnalyticsController(UserService userStore, MaterialService materialStore)
        {
            _userStore = userStore;
            _materialStore = materialStore;
        }

        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = _userStore.FindById(userId);
            var balance = user?.Balance ?? 0;

            var myMaterials = _materialStore.GetByAuthor(userId);
            var materialsCount = myMaterials.Count;

            var transactions = _userStore.GetTransactionsForUser(userId).ToList();

            var premiumViewsCount = transactions
                .Count(t => t.Type == "ViewPremiumMaterial");

            // Материалы по месяцам
            var groupedByMonth = myMaterials
                .GroupBy(m => new { m.CreatedAt.Year, m.CreatedAt.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .ToList();

            var monthLabels = groupedByMonth
                .Select(g => $"{g.Key.Month:00}.{g.Key.Year}")
                .ToList();

            var monthValues = groupedByMonth
                .Select(g => g.Count())
                .ToList();

            // Баллы по времени (накопительно)
            var dateGroups = transactions
                .OrderBy(t => t.CreatedAt)
                .GroupBy(t => t.CreatedAt.Date)
                .ToList();

            var pointsLabels = new List<string>();
            var pointsValues = new List<int>();
            int cumulative = 0;

            foreach (var g in dateGroups)
            {
                cumulative += g.Sum(t => t.Points);
                pointsLabels.Add(g.Key.ToString("dd.MM"));
                pointsValues.Add(cumulative);
            }

            var model = new AnalyticsViewModel
            {
                MaterialsCount = materialsCount,
                Balance = balance,
                PremiumViewsCount = premiumViewsCount,
                MaterialsPerMonthLabels = monthLabels,
                MaterialsPerMonthValues = monthValues,
                PointsHistoryLabels = pointsLabels,
                PointsHistoryValues = pointsValues
            };

            return View(model);
        }
    }
}
