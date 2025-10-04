// Data/InterviewContext.cs
using Microsoft.EntityFrameworkCore;
using InterviewService.Models;

namespace InterviewService.Data;

public class InterviewContext : DbContext
{
    public InterviewContext(DbContextOptions<InterviewContext> options) : base(options) { }

    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<InterviewFeedback> Feedbacks => Set<InterviewFeedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Interview>(b =>
        {
            b.HasKey(i => i.Id);
            b.Property(i => i.Status).HasMaxLength(50);
        });

        modelBuilder.Entity<InterviewFeedback>(b =>
        {
            b.HasKey(f => f.Id);
            b.HasOne(f => f.Interview)
                .WithMany(i => i.Feedbacks)
                .HasForeignKey(f => f.InterviewId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}