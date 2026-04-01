using System.Collections.Generic;

namespace прпгр
{
    public class AnalyticsViewModel
    {
        public int MaterialsCount { get; set; }
        public int Balance { get; set; }
        public int PremiumViewsCount { get; set; }

        public List<string> MaterialsPerMonthLabels { get; set; } = new();
        public List<int> MaterialsPerMonthValues { get; set; } = new();

        public List<string> PointsHistoryLabels { get; set; } = new();
        public List<int> PointsHistoryValues { get; set; } = new();
    }
}
