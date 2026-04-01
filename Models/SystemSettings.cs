namespace прпгр.Models
{
    public class SystemSettings
    {
        public int Id { get; set; }
        public int UploadApprovedReward { get; set; } = 10;
        public int RateMaterialReward { get; set; } = 1;
        public int DailyRatingLimit { get; set; } = 20;
        public int PremiumViewCost { get; set; } = 5;
        public int PlagiarismPenalty { get; set; } = 20;
        public int MaxViolationsBeforeBlock { get; set; } = 3;
    }
}
