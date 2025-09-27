using ats_recruitment_service.Models;
using Microsoft.EntityFrameworkCore;

namespace ats_recruitment_service.Data
{
    public class RecruitmentContext : DbContext
    {
        public RecruitmentContext(DbContextOptions<RecruitmentContext> options)
            : base(options) { }

        public DbSet<Application> Applications { get; set; } = null!;
        public DbSet<Feedback> Feedbacks { get; set; } = null!;
        public DbSet<Offer> Offers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Application>()
                .HasMany(a => a.Feedbacks)
                .WithOne(f => f.Application)
                .HasForeignKey(f => f.ApplicationId);

            modelBuilder.Entity<Application>()
                .HasMany(a => a.Offers)                  // 🔥
                .WithOne(o => o.Application)
                .HasForeignKey(o => o.ApplicationId);
        }
    }
}