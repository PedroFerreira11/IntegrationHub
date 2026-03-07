using IntegrationHub.Domain.Entities;
using IntegrationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.Infrastructure.Logging;

public sealed class RunLogService
{
    private readonly IntegrationHubDbContext _db;

    public RunLogService(IntegrationHubDbContext db) => _db = db;

    public async Task AppendAsync(IEnumerable<IntegrationLog> logs, CancellationToken ct)
    {
        _db.IntegrationLogs.AddRange(logs);
        await _db.SaveChangesAsync(ct);
    }
}