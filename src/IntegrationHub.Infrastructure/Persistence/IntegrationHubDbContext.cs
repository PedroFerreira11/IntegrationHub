using IntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.Infrastructure.Persistence;

public class IntegrationHubDbContext : DbContext
{
    public IntegrationHubDbContext(DbContextOptions<IntegrationHubDbContext> options) : base(options) { }

    public DbSet<SystemEndpoint> SystemEndpoints => Set<SystemEndpoint>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<IntegrationRun> IntegrationRuns => Set<IntegrationRun>();
    public DbSet<IntegrationLog> IntegrationLogs => Set<IntegrationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SystemEndpoint>().HasKey(e => e.Id);
        modelBuilder.Entity<Integration>().HasKey(e => e.Id);
        modelBuilder.Entity<IntegrationRun>().HasKey(e => e.Id);
        modelBuilder.Entity<IntegrationLog>().HasKey(e => e.Id);

        // Integration -> SystemEndpoint
        modelBuilder.Entity<Integration>()
            .HasOne(i => i.SourceEndpoint)
            .WithMany(e => e.SourceIntegrations)
            .HasForeignKey(i => i.SourceEndpointId)
            .OnDelete(DeleteBehavior.Restrict);

        // Integration -> SystemEndpoint (Target)
        modelBuilder.Entity<Integration>()
            .HasOne(i => i.TargetEndpoint)
            .WithMany(e => e.TargetIntegrations)
            .HasForeignKey(i => i.TargetEndpointId)
            .OnDelete(DeleteBehavior.Restrict);

        // Integration -> Runs
        modelBuilder.Entity<IntegrationRun>()
            .HasOne(r => r.Integration)
            .WithMany(i => i.Runs)
            .HasForeignKey(r => r.IntegrationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Run -> Logs
        modelBuilder.Entity<IntegrationLog>()
            .HasOne(l => l.IntegrationRun)
            .WithMany(r => r.Logs)
            .HasForeignKey(l => l.IntegrationRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}