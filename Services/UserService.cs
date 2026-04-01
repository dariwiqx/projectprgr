using System;
using System.Collections.Generic;
using System.Linq;
using прпгр.Data;
using прпгр.Models;

namespace прпгр.Services
{
    public class UserService
    {
        private readonly AppDbContext _db;

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        // ---------- User methods ----------

        public AppUser? Find(string userName, string password)
        {
            return _db.Users.FirstOrDefault(u =>
                u.UserName == userName && u.Password == password);
        }

        public AppUser? FindById(string id) => _db.Users.FirstOrDefault(u => u.Id == id);

        public bool Exists(string userName)
        {
            return _db.Users.Any(u => u.UserName == userName);
        }

        public AppUser Add(string userName, string password)
        {
            if (string.Equals(userName, "admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Пользователь 'admin' зарезервирован.");
            }

            var maxId = _db.Users.Any()
                ? _db.Users.AsEnumerable()
                    .Select(u => int.TryParse(u.Id, out var val) ? val : 0)
                    .DefaultIfEmpty(0)
                    .Max()
                : 0;

            var user = new AppUser
            {
                Id = (maxId + 1).ToString(),
                UserName = userName,
                Password = password,
                Role = "Student",
                Balance = 0,
                IsBlocked = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            _db.SaveChanges();
            return user;
        }

        public List<AppUser> GetAllUsers() => _db.Users.ToList();

        public void ChangeRole(string userId, string role)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.Role = role;
                _db.SaveChanges();
            }
        }

        public void BlockUser(string userId)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.IsBlocked = true;
                _db.SaveChanges();
            }
        }

        public void UnblockUser(string userId)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.IsBlocked = false;
                _db.SaveChanges();
            }
        }

        // ---------- Balance & Transactions ----------

        public void ChangeBalance(string userId, int delta)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.Balance += delta;
                _db.SaveChanges();
            }
        }

        public void AddTransaction(RewardTransaction transaction)
        {
            transaction.CreatedAt = DateTime.UtcNow;
            _db.RewardTransactions.Add(transaction);
            _db.SaveChanges();
        }

        public IEnumerable<RewardTransaction> GetTransactionsForUser(string userId)
        {
            return _db.RewardTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }

        public IReadOnlyCollection<RewardTransaction> GetAllTransactions()
        {
            return _db.RewardTransactions.ToList();
        }

        public bool HasPaidForPremium(string userId, int materialId)
        {
            return _db.RewardTransactions.Any(t =>
                t.UserId == userId &&
                t.MaterialId == materialId &&
                t.Type == "ViewPremiumMaterial" &&
                t.Points < 0);
        }

        public void RewardForRating(string userId, int materialId)
        {
            var settings = GetSettings();
            var today = DateTime.UtcNow.Date;
            var rateTodayCount = _db.RewardTransactions
                .Where(t => t.UserId == userId
                            && t.Type == "RateMaterial"
                            && t.CreatedAt.Date == today)
                .Count();

            if (rateTodayCount >= settings.DailyRatingLimit)
                return;

            AddTransaction(new RewardTransaction
            {
                UserId = userId,
                Type = "RateMaterial",
                Points = settings.RateMaterialReward,
                MaterialId = materialId
            });

            ChangeBalance(userId, +settings.RateMaterialReward);
        }

        // ---------- User Activity ----------

        public void AddActivity(UserActivity activity)
        {
            activity.CreatedAt = DateTime.UtcNow;
            _db.UserActivities.Add(activity);
            _db.SaveChanges();
        }

        public List<UserActivity> GetActivitiesForUser(string userId)
            => _db.UserActivities.Where(a => a.UserId == userId).ToList();

        // ---------- LMS Links ----------

        public void AddLmsLink(LMSAccountLink link)
        {
            link.CreatedAt = DateTime.UtcNow;
            _db.LMSAccountLinks.Add(link);
            _db.SaveChanges();
        }

        public List<LMSAccountLink> GetLmsLinksForUser(string userId)
            => _db.LMSAccountLinks.Where(l => l.UserId == userId && l.IsActive).ToList();

        public LMSAccountLink? GetLmsLinkById(int id)
            => _db.LMSAccountLinks.FirstOrDefault(l => l.Id == id);

        public void RemoveLmsLink(int id)
        {
            var link = _db.LMSAccountLinks.FirstOrDefault(l => l.Id == id);
            if (link != null)
            {
                link.IsActive = false;
                _db.SaveChanges();
            }
        }

        // ---------- System Settings ----------

        public SystemSettings GetSettings()
        {
            var settings = _db.SystemSettings.FirstOrDefault();
            return settings ?? new SystemSettings();
        }

        public void UpdateSettings(SystemSettings settings)
        {
            var existing = _db.SystemSettings.FirstOrDefault();
            if (existing != null)
            {
                existing.UploadApprovedReward = settings.UploadApprovedReward;
                existing.RateMaterialReward = settings.RateMaterialReward;
                existing.DailyRatingLimit = settings.DailyRatingLimit;
                existing.PremiumViewCost = settings.PremiumViewCost;
                existing.PlagiarismPenalty = settings.PlagiarismPenalty;
                existing.MaxViolationsBeforeBlock = settings.MaxViolationsBeforeBlock;
            }
            else
            {
                _db.SystemSettings.Add(settings);
            }
            _db.SaveChanges();
        }
    }
}
