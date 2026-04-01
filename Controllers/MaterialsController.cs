using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using прпгр.Models;
using прпгр.Services;

namespace прпгр.Controllers
{
    public class MaterialsController : Controller
    {
        private readonly MaterialService _store;
        private readonly UserService _userStore;
        private readonly IWebHostEnvironment _env;
        private readonly SemanticSearchService _searchService;

        public MaterialsController(
            MaterialService store,
            UserService userStore,
            IWebHostEnvironment env,
            SemanticSearchService searchService)
        {
            _store = store;
            _userStore = userStore;
            _env = env;
            _searchService = searchService;
        }

        public IActionResult All(
            string? query,
            string? subject,
            string? course,
            string? tag,
            bool onlyPremium = false,
            int? minRating = null,
            string? mode = "keyword",
            int page = 1)
        {
            const int pageSize = 20;
            var materials = _store.GetAll();

            // Role restrictions
            if (!User.IsInRole("Admin") && !User.IsInRole("Moderator"))
            {
                materials = materials.Where(m => m.Status == "Approved").ToList();
            }

            // Filters
            if (!string.IsNullOrWhiteSpace(subject))
                materials = materials.Where(m => m.Subject == subject).ToList();

            if (!string.IsNullOrWhiteSpace(course))
                materials = materials.Where(m => m.Course == course).ToList();

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var filteredIds = _store.GetMaterialsByTag(tag).Select(m => m.Id).ToHashSet();
                materials = materials.Where(m => filteredIds.Contains(m.Id)).ToList();
            }

            if (onlyPremium)
                materials = materials.Where(m => m.IsPremium).ToList();

            if (minRating.HasValue)
                materials = materials.Where(m => m.RatingsCount > 0 && m.AverageRating >= minRating.Value).ToList();

            // Text search
            if (!string.IsNullOrWhiteSpace(query))
            {
                if (mode == "semantic")
                {
                    // Semantic search
                    var results = _searchService.Search(query);
                    var resultIds = results.Select(r => r.MaterialId).ToList();
                    var resultSet = resultIds.ToHashSet();
                    materials = materials.Where(m => resultSet.Contains(m.Id)).ToList();
                    // Sort by score
                    var scoreMap = results.ToDictionary(r => r.MaterialId, r => r.Score);
                    materials = materials.OrderByDescending(m => scoreMap.GetValueOrDefault(m.Id, 0)).ToList();
                }
                else
                {
                    var q = query.Trim();
                    materials = materials.Where(m =>
                        (!string.IsNullOrEmpty(m.Title) &&
                         m.Title.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(m.Description) &&
                         m.Description.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(m.Topic) &&
                         m.Topic.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        _store.GetTagsForMaterial(m.Id)
                              .Any(t => t.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }
            }

            // Pagination
            var totalItems = materials.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var paged = materials.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var model = paged.Select(m => new MaterialViewModel
            {
                Id = m.Id,
                Title = m.Title,
                Subject = m.Subject,
                Course = m.Course,
                Topic = m.Topic,
                ShortDescription = m.Description,
                IsPremium = m.IsPremium,
                Status = m.Status,
                AverageRating = m.AverageRating,
                RatingsCount = m.RatingsCount,
                Tags = _store.GetTagsForMaterial(m.Id).Select(t => t.Name).ToList()
            }).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Query = query;
            ViewBag.Subject = subject;
            ViewBag.Course = course;
            ViewBag.Tag = tag;
            ViewBag.OnlyPremium = onlyPremium;
            ViewBag.MinRating = minRating;
            ViewBag.Mode = mode;

            return View(model);
        }

        [Authorize]
        public IActionResult My()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var materials = _store.GetByAuthor(userId);

            var model = materials
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new MyMaterialViewModel
                {
                    Id = m.Id,
                    Title = m.Title,
                    Subject = m.Subject,
                    Course = m.Course,
                    Topic = m.Topic,
                    ShortDescription = !string.IsNullOrEmpty(m.Description) && m.Description.Length > 200
                        ? m.Description.Substring(0, 200) + "..."
                        : m.Description,
                    Status = m.Status,
                    StatusText = m.Status,
                    CreatedAt = m.CreatedAt,
                    IsPremium = m.IsPremium,
                    AverageRating = m.AverageRating,
                    RatingsCount = m.RatingsCount
                })
                .ToList();

            return View(model);
        }

        [Authorize]
        public IActionResult Create()
        {
            return View(new CreateMaterialViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMaterialViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError("File", "Нужно выбрать файл.");
                return View(model);
            }

            const long maxSize = 20L * 1024 * 1024;
            if (model.File.Length > maxSize)
            {
                ModelState.AddModelError("File", "Файл слишком большой (максимум 20 МБ).");
                return View(model);
            }

            var allowedExtensions = new[] { ".pdf", ".docx", ".pptx", ".png", ".jpeg", ".jpg" };
            var ext = Path.GetExtension(model.File.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                ModelState.AddModelError("File", "Разрешены только PDF, DOCX, PPTX, PNG, JPEG.");
                return View(model);
            }

            var uploadRoot = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadRoot))
                Directory.CreateDirectory(uploadRoot);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadRoot, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var material = new Material
            {
                Title = model.Title,
                Description = model.Description,
                Subject = model.Subject,
                Course = model.Course,
                Topic = model.Topic,
                FilePath = $"/uploads/{fileName}",
                Status = "Pending",
                AuthorId = userId,
                CreatedAt = DateTime.UtcNow,
                IsPremium = model.IsPremium
            };

            _store.Add(material);

            var tags = (model.TagsInput ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t));

            _store.SetTagsForMaterial(material, tags);

            // Rebuild search index
            _searchService.RebuildIndex();

            return RedirectToAction(nameof(My));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Rate(int id, int score, string? comment)
        {
            if (score < 1 || score > 5)
            {
                TempData["RatingError"] = "Оценка должна быть от 1 до 5.";
                return RedirectToAction("Details", new { id });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var material = _store.GetById(id);
            if (material == null) return NotFound();

            if (material.AuthorId == userId)
            {
                TempData["RatingError"] = "Нельзя оценивать собственный материал.";
                return RedirectToAction("Details", new { id });
            }

            if (material.Status != "Approved")
            {
                TempData["RatingError"] = "Можно оценивать только одобренные материалы.";
                return RedirectToAction("Details", new { id });
            }

            _store.AddOrUpdateRating(new MaterialRating
            {
                MaterialId = id,
                UserId = userId,
                Score = score,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
            });

            // Track activity
            _userStore.AddActivity(new UserActivity
            {
                UserId = userId,
                MaterialId = id,
                ActivityType = "Rate"
            });

            var settings = _userStore.GetSettings();
            var today = DateTime.UtcNow.Date;
            var rateTodayCount = _userStore.GetAllTransactions()
                .Where(t => t.UserId == userId
                            && t.Type == "RateMaterial"
                            && t.CreatedAt.Date == today)
                .Count();

            if (rateTodayCount < settings.DailyRatingLimit)
            {
                _userStore.AddTransaction(new RewardTransaction
                {
                    UserId = userId,
                    Type = "RateMaterial",
                    Points = settings.RateMaterialReward,
                    MaterialId = id
                });
                _userStore.ChangeBalance(userId, settings.RateMaterialReward);
            }
            else
            {
                TempData["RatingInfo"] = "Оценка сохранена, но лимит начисления баллов за оценки на сегодня уже достигнут.";
            }

            return RedirectToAction("Details", new { id });
        }

        public IActionResult Details(int id)
        {
            var material = _store.GetById(id);
            if (material == null) return NotFound();

            // Track view activity
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    _userStore.AddActivity(new UserActivity
                    {
                        UserId = userId,
                        MaterialId = id,
                        ActivityType = "View"
                    });
                }
            }

            var tags = _store.GetTagsForMaterial(id).Select(t => t.Name).ToList();
            var ratings = _store.GetRatingsForMaterial(id).ToList();
            var complaints = _store.GetComplaintsForMaterial(id);

            var model = new MaterialDetailsViewModel
            {
                Id = material.Id,
                Title = material.Title,
                Description = material.Description,
                Subject = material.Subject,
                Course = material.Course,
                Topic = material.Topic,
                FilePath = material.FilePath,
                Tags = tags,
                AverageRating = material.AverageRating,
                RatingsCount = material.RatingsCount,
                Ratings = ratings,
                Status = material.Status,
                IsPremium = material.IsPremium,
                AuthorId = material.AuthorId,
                ComplaintsCount = complaints.Count
            };

            return View(model);
        }

        [Authorize]
        public IActionResult Open(int id)
        {
            var material = _store.GetById(id);
            if (material == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Track activity and download count
            _userStore.AddActivity(new UserActivity
            {
                UserId = userId,
                MaterialId = id,
                ActivityType = "Download"
            });

            material.DownloadCount++;
            _store.Update(material);

            if (!material.IsPremium)
            {
                return Redirect(material.FilePath);
            }

            var settings = _userStore.GetSettings();

            if (!_userStore.HasPaidForPremium(userId, id))
            {
                var user = _userStore.FindById(userId);
                var currentBalance = user?.Balance ?? 0;

                if (currentBalance < settings.PremiumViewCost)
                {
                    TempData["PremiumError"] = $"Недостаточно баллов для просмотра премиум-материала (нужно {settings.PremiumViewCost} баллов).";
                    return RedirectToAction("Details", new { id });
                }

                _userStore.AddTransaction(new RewardTransaction
                {
                    UserId = userId,
                    Type = "ViewPremiumMaterial",
                    Points = -settings.PremiumViewCost,
                    MaterialId = id
                });
                _userStore.ChangeBalance(userId, -settings.PremiumViewCost);

                TempData["PremiumInfo"] = $"За просмотр премиум-материала списано {settings.PremiumViewCost} баллов. Повторные просмотры этого материала бесплатны.";
            }

            return Redirect(material.FilePath);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var material = _store.GetById(id);
            if (material == null) return NotFound();

            if (material.AuthorId != userId && !User.IsInRole("Admin"))
                return Forbid();

            if (material.Status != "Pending" && material.Status != "Rejected" && !User.IsInRole("Admin"))
            {
                TempData["DeleteError"] = "Удалять можно только материалы со статусом 'Ожидает' или 'Отклонён'.";
                return RedirectToAction(nameof(My));
            }

            _store.Delete(id);
            TempData["DeleteSuccess"] = "Материал удалён.";
            return RedirectToAction(nameof(My));
        }
    }
}
