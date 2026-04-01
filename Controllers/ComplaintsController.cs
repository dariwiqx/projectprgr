using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using прпгр.Models;
using прпгр.Services;

namespace прпгр.Controllers
{
    public class ComplaintsController : Controller
    {
        private readonly MaterialService _store;

        public ComplaintsController(MaterialService store)
        {
            _store = store;
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int materialId, string reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ComplaintError"] = "Укажите причину жалобы.";
                return RedirectToAction("Details", "Materials", new { id = materialId });
            }

            var material = _store.GetById(materialId);
            if (material == null) return NotFound();

            _store.AddComplaint(new Complaint
            {
                MaterialId = materialId,
                UserId = userId,
                Reason = reason.Trim()
            });

            TempData["ComplaintSuccess"] = "Жалоба отправлена. Модераторы рассмотрят её в ближайшее время.";
            return RedirectToAction("Details", "Materials", new { id = materialId });
        }
    }
}
