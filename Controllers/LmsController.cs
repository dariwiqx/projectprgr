using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using прпгр.Models;
using прпгр.Services;

namespace прпгр.Controllers
{
    [Authorize]
    public class LmsController : Controller
    {
        private readonly UserService _userStore;
        private readonly MaterialService _materialStore;
        private readonly LmsService _lmsService;

        public LmsController(
            UserService userStore,
            MaterialService materialStore,
            LmsService lmsService)
        {
            _userStore = userStore;
            _materialStore = materialStore;
            _lmsService = lmsService;
        }

        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var links = _userStore.GetLmsLinksForUser(userId);
            return View(links);
        }

        [HttpGet]
        public IActionResult Link()
        {
            return View(new LmsLinkViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Link(LmsLinkViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (!_lmsService.VerifyConnection(model.LmsUrl, model.LmsToken, model.LmsType))
            {
                ModelState.AddModelError("", "Не удалось подключиться к LMS. Проверьте URL и токен.");
                return View(model);
            }

            _userStore.AddLmsLink(new LMSAccountLink
            {
                UserId = userId,
                LmsUrl = model.LmsUrl,
                LmsToken = model.LmsToken,
                LmsType = model.LmsType
            });

            TempData["LmsSuccess"] = "LMS-аккаунт успешно привязан.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Unlink(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var link = _userStore.GetLmsLinkById(id);
            if (link == null || link.UserId != userId) return NotFound();

            _userStore.RemoveLmsLink(id);
            TempData["LmsSuccess"] = "LMS-аккаунт отвязан.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Courses(int linkId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var link = _userStore.GetLmsLinkById(linkId);
            if (link == null || link.UserId != userId) return NotFound();

            var courses = _lmsService.GetCourses(link);
            ViewBag.LinkId = linkId;
            ViewBag.LmsType = link.LmsType;

            // Get user's approved materials for publishing
            ViewBag.Materials = _materialStore.GetByAuthor(userId)
                .Where(m => m.Status == "Approved")
                .ToList();

            return View(courses);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Publish(int materialId, int linkId, string courseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var link = _userStore.GetLmsLinkById(linkId);
            if (link == null || link.UserId != userId) return NotFound();

            var material = _materialStore.GetById(materialId);
            if (material == null || material.AuthorId != userId) return NotFound();

            var result = _lmsService.PublishMaterial(link, courseId, material);

            if (result.Success)
                TempData["LmsSuccess"] = result.Message;
            else
                TempData["LmsError"] = result.Message;

            return RedirectToAction(nameof(Courses), new { linkId });
        }
    }

    public class LmsLinkViewModel
    {
        public string LmsUrl { get; set; } = "";
        public string LmsToken { get; set; } = "";
        public string LmsType { get; set; } = "Moodle";
    }
}
