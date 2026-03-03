using IntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.Infrastructure.Persistence;

public class IntegrationHubDbContext : DbContext
{
    
    public  IntegrationHubDbContext(DbContextOptions<IntegrationHubDbContext> options) : base(options) {}
    
    public DbSet<SystemEndpoint> SystemEndpoints => Set<SystemEndpoint>();
    public DbSet<Integration> Integration => Set<Integration>();
    public DbSet<IntegrationRun> IntegrationRun => Set<IntegrationRun>();
    public DbSet<IntegrationLog> IntegrationLog => Set<IntegrationLog>();
    
    
}