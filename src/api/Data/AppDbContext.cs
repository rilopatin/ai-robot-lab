using Microsoft.EntityFrameworkCore;
using OpportunityOs.Api.Domain;

namespace OpportunityOs.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<Experiment> Experiments => Set<Experiment>();
    public DbSet<Evidence> Evidence => Set<Evidence>();
    public DbSet<Decision> Decisions => Set<Decision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Goal>(entity =>
        {
            entity.Property(goal => goal.Status).HasConversion<string>();
            entity.HasMany(goal => goal.Opportunities)
                .WithOne(opportunity => opportunity.Goal)
                .HasForeignKey(opportunity => opportunity.GoalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Opportunity>(entity =>
        {
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Opportunity_ProbabilityToFirstDollar",
                "ProbabilityToFirstDollar >= 0 AND ProbabilityToFirstDollar <= 100"));
            entity.HasMany(opportunity => opportunity.Experiments)
                .WithOne(experiment => experiment.Opportunity)
                .HasForeignKey(experiment => experiment.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Experiment>(entity =>
        {
            entity.Property(experiment => experiment.Status).HasConversion<string>();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Experiment_BudgetCents", "BudgetCents = 0");
                table.HasCheckConstraint("CK_Experiment_TimeBudgetMinutes", "TimeBudgetMinutes >= 0");
            });
            entity.HasMany(experiment => experiment.Evidence)
                .WithOne(evidence => evidence.Experiment)
                .HasForeignKey(evidence => evidence.ExperimentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(experiment => experiment.Decision)
                .WithOne(decision => decision.Experiment)
                .HasForeignKey<Decision>(decision => decision.ExperimentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Evidence>(entity =>
        {
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Evidence_Views", "Views IS NULL OR Views >= 0");
                table.HasCheckConstraint("CK_Evidence_Installs", "Installs IS NULL OR Installs >= 0");
                table.HasCheckConstraint("CK_Evidence_Downloads", "Downloads IS NULL OR Downloads >= 0");
                table.HasCheckConstraint("CK_Evidence_Users", "Users IS NULL OR Users >= 0");
                table.HasCheckConstraint("CK_Evidence_Clicks", "Clicks IS NULL OR Clicks >= 0");
                table.HasCheckConstraint("CK_Evidence_Sales", "Sales IS NULL OR Sales >= 0");
                table.HasCheckConstraint("CK_Evidence_RevenueCents", "RevenueCents >= 0");
            });
        });

        modelBuilder.Entity<Decision>(entity =>
        {
            entity.Property(decision => decision.Outcome).HasConversion<string>();
            entity.HasIndex(decision => decision.ExperimentId).IsUnique();
        });
    }
}
