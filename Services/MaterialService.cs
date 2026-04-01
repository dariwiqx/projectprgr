using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using прпгр.Data;
using прпгр.Models;

namespace прпгр.Services
{
    public class MaterialService
    {
        private readonly AppDbContext _db;

        public MaterialService(AppDbContext db)
        {
            _db = db;
        }

        // ---------- Materials ----------

        public List<Material> GetAll() => _db.Materials.ToList();

        public Material? GetById(int id) => _db.Materials.FirstOrDefault(m => m.Id == id);

        public List<Material> GetByAuthor(string authorId) =>
            _db.Materials.Where(m => m.AuthorId == authorId).ToList();

        public void Add(Material material)
        {
            _db.Materials.Add(material);
            _db.SaveChanges();
        }

        public void Update(Material material)
        {
            var existing = _db.Materials.FirstOrDefault(m => m.Id == material.Id);
            if (existing != null)
            {
                existing.Title = material.Title;
                existing.Description = material.Description;
                existing.Subject = material.Subject;
                existing.Course = material.Course;
                existing.Topic = material.Topic;
                existing.FilePath = material.FilePath;
                existing.Status = material.Status;
                existing.IsPremium = material.IsPremium;
                existing.CreatedAt = material.CreatedAt;
                existing.AuthorId = material.AuthorId;
                existing.AverageRating = material.AverageRating;
                existing.RatingsCount = material.RatingsCount;
                existing.DownloadCount = material.DownloadCount;
                _db.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            var material = _db.Materials.FirstOrDefault(m => m.Id == id);
            if (material != null)
            {
                // Remove related data
                var tags = _db.MaterialTags.Where(mt => mt.MaterialId == id);
                _db.MaterialTags.RemoveRange(tags);

                var ratings = _db.MaterialRatings.Where(r => r.MaterialId == id);
                _db.MaterialRatings.RemoveRange(ratings);

                var complaints = _db.Complaints.Where(c => c.MaterialId == id);
                _db.Complaints.RemoveRange(complaints);

                var logs = _db.ModerationLogs.Where(l => l.MaterialId == id);
                _db.ModerationLogs.RemoveRange(logs);

                _db.Materials.Remove(material);
                _db.SaveChanges();
            }
        }

        // ---------- Tags ----------

        public IEnumerable<Tag> GetAllTags() => _db.Tags.ToList();

        public IEnumerable<Tag> GetTagsForMaterial(int materialId)
        {
            var tagIds = _db.MaterialTags
                .Where(mt => mt.MaterialId == materialId)
                .Select(mt => mt.TagId)
                .ToHashSet();

            return _db.Tags.Where(t => tagIds.Contains(t.Id)).ToList();
        }

        public void SetTagsForMaterial(Material material, IEnumerable<string> tagNames)
        {
            var existing = _db.MaterialTags.Where(mt => mt.MaterialId == material.Id);
            _db.MaterialTags.RemoveRange(existing);

            foreach (var rawName in tagNames)
            {
                var name = rawName.Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                var tag = _db.Tags.FirstOrDefault(t => t.Name.ToLower() == name.ToLower());

                if (tag == null)
                {
                    tag = new Tag { Name = name };
                    _db.Tags.Add(tag);
                    _db.SaveChanges();
                }

                if (!_db.MaterialTags.Any(mt => mt.MaterialId == material.Id && mt.TagId == tag.Id))
                {
                    _db.MaterialTags.Add(new MaterialTag
                    {
                        MaterialId = material.Id,
                        TagId = tag.Id
                    });
                }
            }

            _db.SaveChanges();
        }

        public IEnumerable<Material> GetMaterialsByTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
                return Enumerable.Empty<Material>();

            var tag = _db.Tags.FirstOrDefault(t => t.Name.ToLower() == tagName.ToLower());
            if (tag == null) return Enumerable.Empty<Material>();

            var materialIds = _db.MaterialTags
                .Where(mt => mt.TagId == tag.Id)
                .Select(mt => mt.MaterialId)
                .ToHashSet();

            return _db.Materials.Where(m => materialIds.Contains(m.Id)).ToList();
        }

        // ---------- Ratings ----------

        public IEnumerable<MaterialRating> GetRatingsForMaterial(int materialId)
            => _db.MaterialRatings.Where(r => r.MaterialId == materialId).ToList();

        public MaterialRating? GetRatingForUser(int materialId, string userId)
            => _db.MaterialRatings.FirstOrDefault(r => r.MaterialId == materialId && r.UserId == userId);

        public void AddOrUpdateRating(MaterialRating rating)
        {
            var existing = GetRatingForUser(rating.MaterialId, rating.UserId);
            if (existing == null)
            {
                rating.CreatedAt = DateTime.UtcNow;
                _db.MaterialRatings.Add(rating);
            }
            else
            {
                existing.Score = rating.Score;
                existing.Comment = rating.Comment;
                existing.CreatedAt = DateTime.UtcNow;
            }

            _db.SaveChanges();
            RecalculateMaterialRating(rating.MaterialId);
        }

        private void RecalculateMaterialRating(int materialId)
        {
            var material = _db.Materials.FirstOrDefault(m => m.Id == materialId);
            if (material == null) return;

            var ratings = _db.MaterialRatings.Where(r => r.MaterialId == materialId).ToList();
            if (ratings.Count == 0)
            {
                material.AverageRating = 0;
                material.RatingsCount = 0;
            }
            else
            {
                material.RatingsCount = ratings.Count;
                material.AverageRating = ratings.Average(r => r.Score);
            }

            _db.SaveChanges();
        }

        // ---------- Complaints ----------

        public void AddComplaint(Complaint complaint)
        {
            complaint.CreatedAt = DateTime.UtcNow;
            _db.Complaints.Add(complaint);
            _db.SaveChanges();
        }

        public List<Complaint> GetComplaintsForMaterial(int materialId)
            => _db.Complaints.Where(c => c.MaterialId == materialId).ToList();

        public List<Complaint> GetAllComplaints()
            => _db.Complaints.ToList();

        public void ResolveComplaint(int complaintId)
        {
            var complaint = _db.Complaints.FirstOrDefault(c => c.Id == complaintId);
            if (complaint != null)
            {
                complaint.IsResolved = true;
                _db.SaveChanges();
            }
        }

        // ---------- Moderation Logs ----------

        public void AddModerationLog(ModerationLog log)
        {
            log.CreatedAt = DateTime.UtcNow;
            _db.ModerationLogs.Add(log);
            _db.SaveChanges();
        }

        public List<ModerationLog> GetModerationLogsForMaterial(int materialId)
            => _db.ModerationLogs.Where(l => l.MaterialId == materialId).ToList();

        public List<ModerationLog> GetAllModerationLogs()
            => _db.ModerationLogs.ToList();

        // ---------- Plagiarism Detection ----------

        public List<Material> GetSuspiciousMaterials()
        {
            var approved = _db.Materials
                .Where(m => m.Status == "Approved" || m.Status == "Pending")
                .ToList();
            var suspicious = new HashSet<int>();

            for (int i = 0; i < approved.Count; i++)
            {
                for (int j = i + 1; j < approved.Count; j++)
                {
                    var a = approved[i];
                    var b = approved[j];

                    if (a.AuthorId == b.AuthorId) continue;

                    if (!string.IsNullOrEmpty(a.Title) && !string.IsNullOrEmpty(b.Title) &&
                        a.Title.Equals(b.Title, StringComparison.OrdinalIgnoreCase))
                    {
                        suspicious.Add(a.Id);
                        suspicious.Add(b.Id);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(a.Title) && !string.IsNullOrEmpty(b.Title) &&
                        LevenshteinDistance(a.Title.ToLower(), b.Title.ToLower()) < 3)
                    {
                        suspicious.Add(a.Id);
                        suspicious.Add(b.Id);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(a.Subject) && !string.IsNullOrEmpty(b.Subject) &&
                        !string.IsNullOrEmpty(a.Topic) && !string.IsNullOrEmpty(b.Topic) &&
                        a.Subject.Equals(b.Subject, StringComparison.OrdinalIgnoreCase) &&
                        a.Topic.Equals(b.Topic, StringComparison.OrdinalIgnoreCase))
                    {
                        var tagsA = GetTagsForMaterial(a.Id).Select(t => t.Name.ToLower()).ToHashSet();
                        var tagsB = GetTagsForMaterial(b.Id).Select(t => t.Name.ToLower()).ToHashSet();
                        if (tagsA.Count > 0 && tagsB.Count > 0 && tagsA.SetEquals(tagsB))
                        {
                            suspicious.Add(a.Id);
                            suspicious.Add(b.Id);
                        }
                    }
                }
            }

            return _db.Materials.Where(m => suspicious.Contains(m.Id)).ToList();
        }

        public int GetPendingCount()
        {
            return _db.Materials.Count(m => m.Status == "Pending");
        }

        public int GetUnresolvedComplaintsCount()
        {
            return _db.Complaints.Count(c => !c.IsResolved);
        }

        private static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            var d = new int[s.Length + 1, t.Length + 1];

            for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= t.Length; j++) d[0, j] = j;

            for (int i = 1; i <= s.Length; i++)
            {
                for (int j = 1; j <= t.Length; j++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[s.Length, t.Length];
        }
    }
}
