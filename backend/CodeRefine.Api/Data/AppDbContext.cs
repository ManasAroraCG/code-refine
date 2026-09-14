using CodeRefine.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeRefine.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<AnalysisRun> AnalysisRuns => Set<AnalysisRun>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<Patch> Patches => Set<Patch>();
    public DbSet<VerificationResult> VerificationResults => Set<VerificationResult>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GitHubLogin).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.GitHubLogin).IsUnique();
        });

        modelBuilder.Entity<Repository>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.FullName);
            entity.Property(e => e.Owner).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.GitHubRepoId).HasMaxLength(100);
            entity.HasIndex(e => new { e.Owner, e.Name }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(u => u.Repositories)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AnalysisRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RepositoryId, e.StartedAt });
            entity.HasOne(e => e.Repository)
                .WithMany(r => r.AnalysisRuns)
                .HasForeignKey(e => e.RepositoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Finding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AgentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => e.AnalysisRunId);
            entity.HasOne(e => e.AnalysisRun)
                .WithMany(a => a.Findings)
                .HasForeignKey(e => e.AnalysisRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Patch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => e.FindingId);
            entity.HasOne(e => e.Finding)
                .WithMany(f => f.Patches)
                .HasForeignKey(e => e.FindingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VerificationResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AnalysisRunId, e.CheckType });
            entity.HasOne(e => e.AnalysisRun)
                .WithMany(a => a.VerificationResults)
                .HasForeignKey(e => e.AnalysisRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AnalysisRunId);
            entity.HasOne(e => e.AnalysisRun)
                .WithMany(a => a.Reviews)
                .HasForeignKey(e => e.AnalysisRunId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Finding)
                .WithMany(f => f.Reviews)
                .HasForeignKey(e => e.FindingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Patch)
                .WithMany(p => p.Reviews)
                .HasForeignKey(e => e.PatchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
