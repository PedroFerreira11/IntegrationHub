using IntegrationHub.Application.Runs;
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
    public async Task<ActionResult> Run(Guid integrationId, CancellationToken ct)
    {
        try
        {
            var result = await _runner.RunAsync(integrationId, ct);
            return Ok(new { runId = result.IntegrationId, status = result.Status });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
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
            run.StartedAt,
            run.FinishedAt,
            Status = run.Status.ToString(),
            run.ErrorMessage
        });
    }

    [HttpGet("/api/runs/{runId:guid}/logs")]
    public async Task<ActionResult> GetLogs(Guid runId, CancellationToken ct)
    {
        var logs = await _db.IntegrationLogs
            .AsNoTracking()
            .Where(l => l.IntegrationRunId == runId)
            .OrderBy(l => l.Timestamp)
            .Select(l => new { l.Timestamp, Level = l.Level.ToString(), l.Message })
            .ToListAsync(ct);

        return Ok(logs);
    }
}