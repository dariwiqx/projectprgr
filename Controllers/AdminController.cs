using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using прпгр.Models;
using прпгр.Services;

namespace прпгр.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserService _userStore;

        public AdminController(UserService userStore)
        {
            _userStore = userStore;
        }

        public IActionResult Users()
        {
            var users = _userStore.GetAllUsers()
                .OrderBy(u => u.Role)
                .ThenBy(u => u.UserName)
                .ToList();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeRole(string userId, string role)
        {
            var validRoles = new[] { "Student", "Moderator", "Admin" };
            if (!validRoles.Contains(role))
            {
                TempData["AdminError"] = "Неверная роль.";
                return RedirectToAction(nameof(Users));
            }

            _userStore.ChangeRole(userId, role);
            TempData["AdminSuccess"] = "Роль изменена.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BlockUser(string userId)
        {
            _userStore.BlockUser(userId);
            TempData["AdminSuccess"] = "Пользователь заблокирован.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnblockUser(string userId)
        {
            _userStore.UnblockUser(userId);
            TempData["AdminSuccess"] = "Пользователь разблокирован.";
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public IActionResult Settings()
        {
            var settings = _userStore.GetSettings();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Settings(SystemSettings model)
        {
            _userStore.UpdateSettings(model);
            TempData["AdminSuccess"] = "Настройки сохранены.";
            return View(model);
        }
    }
}
