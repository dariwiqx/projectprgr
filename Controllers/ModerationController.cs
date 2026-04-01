using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using прпгр.Models;
using прпгр.Services;

namespace прпгр.Controllers
{
    [Authorize(Roles = "Admin,Moderator")]
    public class ModerationController : Controller
    {
        private readonly MaterialService _store;
        private readonly UserService _userStore;

        public ModerationController(MaterialService store, UserService userStore)
        {
            _store = store;
            _userStore = userStore;
        }

        public IActionResult Index(string? filter, string? sort)
        {
            var materials = _store.GetAll();
            var complaints = _store.GetAllComplaints();
            var suspicious = _store.GetSuspiciousMaterials().Select(m => m.Id).ToHashSet();

            var items = materials.Select(m => new ModerationItemViewModel
            {
                Material = m,
                Tags = _store.GetTagsForMaterial(m.Id).Select(t => t.Name).ToList(),
                ComplaintsCount = complaints.Count(c => c.MaterialId == m.Id && !c.IsResolved),
                IsSuspicious = suspicious.Contains(m.Id),
                Complaints = complaints.Where(c => c.MaterialId == m.Id).ToList()
            }).ToList();

            // Filter
            items = filter switch
            {
                "pending" => items.Where(i => i.Material.Status == "Pending").ToList(),
                "complaints" => items.Where(i => i.ComplaintsCount > 0).ToList(),
                "plagiarism" => items.Where(i => i.IsSuspicious).ToList(),
                "blocked" => items.Where(i => i.Material.Status == "Blocked").ToList(),
                _ => items.Where(i => i.Material.Status == "Pending" || i.ComplaintsCount > 0 || i.IsSuspicious).ToList()
            };

            // Sort
            items = sort switch
            {
                "complaints" => items.OrderByDescending(i => i.ComplaintsCount).ToList(),
                "date_asc" => items.OrderBy(i => i.Material.CreatedAt).ToList(),
                _ => items.OrderByDescending(i => i.Material.CreatedAt).ToList()
            };

            ViewBag.Filter = filter;
            ViewBag.Sort = sort;
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id, string? comment)
        {
            var material = _store.GetById(id);
            if (material == null) return NotFound();

            var moderatorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var oldStatus = material.Status;

            material.Status = "Approved";
            _store.Update(material);

            _store.AddModerationLog(new ModerationLog
            {
                MaterialId = id,
                ModeratorId = moderatorId,
                OldStatus = oldStatus,
                NewStatus = "Approved",
                Comment = comment
            });

            var settings = _userStore.GetSettings();
            _userStore.AddTransaction(new RewardTransaction
            {
                UserId = material.AuthorId,
                Type = "UploadApprovedMaterial",
                Points = settings.UploadApprovedReward,
                MaterialId = material.Id
            });
            _userStore.ChangeBalance(material.AuthorId, settings.UploadApprovedReward);

            // Resolve complaints for this material
            foreach (var c in _store.GetComplaintsForMaterial(id))
            {
                _store.ResolveComplaint(c.Id);
            }

            TempData["ModerationSuccess"] = $"Материал \"{material.Title}\" одобрен.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(int id, string? comment)
        {
            var material = _store.GetById(id);
            if (material == null) return NotFound();

            var moderatorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var oldStatus = material.Status;

            material.Status = "Rejected";
            _store.Update(material);

            _store.AddModerationLog(new ModerationLog
            {
                MaterialId = id,
                ModeratorId = moderatorId,
                OldStatus = oldStatus,
                NewStatus = "Rejected",
                Comment = comment
            });

            TempData["ModerationSuccess"] = $"Материал \"{material.Title}\" отклонён.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Block(int id, string? comment)
        {
            var material = _store.GetById(id);
            if (material == null) return NotFound();

            var moderatorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var oldStatus = material.Status;

            material.Status = "Blocked";
            _store.Update(material);

            _store.AddModerationLog(new ModerationLog
            {
                MaterialId = id,
                ModeratorId = moderatorId,
                OldStatus = oldStatus,
                NewStatus = "Blocked",
                Comment = comment
            });

            TempData["ModerationSuccess"] = $"Материал \"{material.Title}\" заблокирован.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmPlagiarism(int id)
        {
            var material = _store.GetById(id);
            if (material == null) return NotFound();

            var moderatorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var oldStatus = material.Status;
            var settings = _userStore.GetSettings();

            material.Status = "Blocked";
            _store.Update(material);

            _store.AddModerationLog(new ModerationLog
            {
                MaterialId = id,
                ModeratorId = moderatorId,
                OldStatus = oldStatus,
                NewStatus = "Blocked",
                Comment = "Подтверждён плагиат"
            });

            // Penalty
            _userStore.AddTransaction(new RewardTransaction
            {
                UserId = material.AuthorId,
                Type = "PlagiarismPenalty",
                Points = -settings.PlagiarismPenalty,
                MaterialId = id
            });
            _userStore.ChangeBalance(material.AuthorId, -settings.PlagiarismPenalty);

            // Check violation count
            var violationCount = _store.GetAllModerationLogs()
                .Count(l => l.NewStatus == "Blocked" && l.Comment == "Подтверждён плагиат" &&
                    _store.GetById(l.MaterialId)?.AuthorId == material.AuthorId);

            if (violationCount >= settings.MaxViolationsBeforeBlock)
            {
                _userStore.BlockUser(material.AuthorId);
                TempData["ModerationSuccess"] = $"Плагиат подтверждён. Автор заблокирован после {violationCount} нарушений.";
            }
            else
            {
                TempData["ModerationSuccess"] = $"Плагиат подтверждён. Штраф -{settings.PlagiarismPenalty} баллов. Нарушений: {violationCount}/{settings.MaxViolationsBeforeBlock}.";
            }

            return RedirectToAction(nameof(Index));
        }
    }

    public class ModerationItemViewModel
    {
        public Material Material { get; set; } = null!;
        public List<string> Tags { get; set; } = new();
        public int ComplaintsCount { get; set; }
        public bool IsSuspicious { get; set; }
        public List<Complaint> Complaints { get; set; } = new();
    }
}
