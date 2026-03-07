using IntegrationHub.Application.Runs;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;
using IntegrationHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.Api.Controllers;

[ApiController]
[Route("api/integrations/{integrationId:guid}/runs")]
public class RunsController : ControllerBase
{
    private readonly IIntegrationRunner _runner;
    private readonly IntegrationHubDbContext _db;

    public RunsController(IIntegrationRunner runner, IntegrationHubDbContext db)
    {
        _runner = runner;
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult> CreateRun(Guid integrationId, CancellationToken ct)
    {
        var integrationExists = await _db.Integrations
            .AnyAsync(i => i.Id == integrationId, ct);

        if (!integrationExists)
            return NotFound(new { error = "Integration not found." });

        var run = new IntegrationRun
        {
            IntegrationId = integrationId,
            Status = RunStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            RetryCount = 0,
            MaxRetries = 3
        };
        
        _db.IntegrationRuns.Add(run);
        await _db.SaveChangesAsync(ct);
        
        return Accepted(new
        {
            run.Id,
            run.IntegrationId,
            Status = run.Status.ToString(),
            run.CreatedAt,
            run.RetryCount,
            run.MaxRetries
        });
    }

    [HttpGet("/api/runs/{runId:guid}")]
    public async Task<ActionResult> GetRun(Guid runId, CancellationToken ct)
    {
        var run = await _db.IntegrationRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId, ct);

        if (run is null) return NotFound();

        return Ok(new
        {
            run.Id,
            run.IntegrationId,
            run.CreatedAt,
            run.StartedAt,
            run.FinishedAt,
            Status = run.Status.ToString(),
            run.ErrorMessage,
            run.RetryCount,
            run.MaxRetries,
            run.NextRetryAt
        });
    }

    [HttpGet("/api/runs/{runId:guid}/logs")]
    public async Task<ActionResult> GetLogs(Guid runId, CancellationToken ct)
    {
        var runExists = await _db.IntegrationRuns
            .AnyAsync(r => r.Id == runId, ct);

        if (!runExists)
            return NotFound(new { error = "Run not found." });

        var logs = await _db.IntegrationLogs
            .AsNoTracking()
            .Where(l => l.IntegrationRunId == runId)
            .OrderBy(l => l.Timestamp)
            .Select(l => new
            {
                l.Timestamp,
                Level = l.Level.ToString(),
                l.Message
            })
            .ToListAsync(ct);

        return Ok(logs);
    }
}