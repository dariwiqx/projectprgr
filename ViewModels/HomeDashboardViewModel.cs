using System.Collections.Generic;

namespace прпгр.Models
{
    public class HomeDashboardViewModel
    {
        public int TotalMaterials { get; set; }
        public int ApprovedMaterials { get; set; }
        public int PremiumViewsCount { get; set; }

        public int MyMaterialsCount { get; set; }
        public int MyPremiumViewsCount { get; set; }
        public int CurrentUserBalance { get; set; }

        public List<MaterialViewModel> RecommendedMaterials { get; set; } = new();
    }
}
