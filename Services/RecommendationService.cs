using System;
using System.Collections.Generic;
using System.Linq;
using прпгр.Models;

namespace прпгр.Services
{
    public class RecommendationService
    {
        private readonly MaterialService _materialStore;
        private readonly UserService _userStore;

        public RecommendationService(MaterialService materialStore, UserService userStore)
        {
            _materialStore = materialStore;
            _userStore = userStore;
        }

        public List<Material> GetRecommendations(string userId, int count = 10)
        {
            var activities = _userStore.GetActivitiesForUser(userId);
            var approvedMaterials = _materialStore.GetAll()
                .Where(m => m.Status == "Approved" && m.AuthorId != userId)
                .ToList();

            if (approvedMaterials.Count == 0)
                return new List<Material>();

            if (activities.Count < 3)
            {
                return approvedMaterials
                    .OrderByDescending(m => m.AverageRating)
                    .ThenByDescending(m => m.RatingsCount)
                    .Take(count)
                    .ToList();
            }

            var viewedMaterialIds = activities
                .Select(a => a.MaterialId)
                .Distinct()
                .ToHashSet();

            var viewedMaterials = _materialStore.GetAll()
                .Where(m => viewedMaterialIds.Contains(m.Id))
                .ToList();

            var preferredSubjects = viewedMaterials
                .Where(m => !string.IsNullOrEmpty(m.Subject))
                .GroupBy(m => m.Subject)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var preferredCourses = viewedMaterials
                .Where(m => !string.IsNullOrEmpty(m.Course))
                .GroupBy(m => m.Course)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var preferredTopics = viewedMaterials
                .Where(m => !string.IsNullOrEmpty(m.Topic))
                .GroupBy(m => m.Topic)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var preferredTags = viewedMaterials
                .SelectMany(m => _materialStore.GetTagsForMaterial(m.Id).Select(t => t.Name))
                .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var candidates = approvedMaterials
                .Where(m => !viewedMaterialIds.Contains(m.Id))
                .Select(m =>
                {
                    double score = 0;
                    if (preferredSubjects.Contains(m.Subject ?? "")) score += 3;
                    if (preferredCourses.Contains(m.Course ?? "")) score += 2;
                    if (preferredTopics.Contains(m.Topic ?? "")) score += 2;

                    var tags = _materialStore.GetTagsForMaterial(m.Id).Select(t => t.Name);
                    score += tags.Count(t => preferredTags.Contains(t));

                    if (m.RatingsCount >= 5 && m.AverageRating >= 3)
                        score += m.AverageRating * 0.5;
                    else if (m.RatingsCount >= 1 && m.AverageRating >= 3)
                        score += m.AverageRating * 0.2;

                    return (Material: m, Score: score);
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(count)
                .Select(x => x.Material)
                .ToList();

            if (candidates.Count < count)
            {
                var existingIds = candidates.Select(c => c.Id).ToHashSet();
                var topRated = approvedMaterials
                    .Where(m => !existingIds.Contains(m.Id) && !viewedMaterialIds.Contains(m.Id))
                    .OrderByDescending(m => m.AverageRating)
                    .Take(count - candidates.Count);
                candidates.AddRange(topRated);
            }

            return candidates;
        }
    }
}
