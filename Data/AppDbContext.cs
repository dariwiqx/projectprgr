using Microsoft.EntityFrameworkCore;
using прпгр.Models;

namespace прпгр.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppUser> Users { get; set; } = null!;
        public DbSet<Material> Materials { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<MaterialTag> MaterialTags { get; set; } = null!;
        public DbSet<MaterialRating> MaterialRatings { get; set; } = null!;
        public DbSet<RewardTransaction> RewardTransactions { get; set; } = null!;
        public DbSet<Complaint> Complaints { get; set; } = null!;
        public DbSet<ModerationLog> ModerationLogs { get; set; } = null!;
        public DbSet<UserActivity> UserActivities { get; set; } = null!;
        public DbSet<LMSAccountLink> LMSAccountLinks { get; set; } = null!;
        public DbSet<SystemSettings> SystemSettings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // AppUser
            modelBuilder.Entity<AppUser>(e =>
            {
                e.HasKey(u => u.Id);
                e.HasIndex(u => u.UserName).IsUnique();
            });

            // MaterialTag composite key
            modelBuilder.Entity<MaterialTag>(e =>
            {
                e.HasKey(mt => new { mt.MaterialId, mt.TagId });
            });

            // Tag unique name
            modelBuilder.Entity<Tag>(e =>
            {
                e.HasIndex(t => t.Name).IsUnique();
            });

            // MaterialRating unique per user+material
            modelBuilder.Entity<MaterialRating>(e =>
            {
                e.HasIndex(r => new { r.MaterialId, r.UserId }).IsUnique();
            });

            // Seed default SystemSettings
            modelBuilder.Entity<SystemSettings>().HasData(new SystemSettings
            {
                Id = 1,
                UploadApprovedReward = 10,
                RateMaterialReward = 1,
                DailyRatingLimit = 20,
                PremiumViewCost = 5,
                PlagiarismPenalty = 20,
                MaxViolationsBeforeBlock = 3
            });

            // Seed admin user
            modelBuilder.Entity<AppUser>().HasData(new AppUser
            {
                Id = "1",
                UserName = "admin",
                Password = "admin",
                Role = "Admin",
                Balance = 0,
                IsBlocked = false
            });
        }
    }
}
