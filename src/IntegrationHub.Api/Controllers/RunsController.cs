using IntegrationHub.Api.Contracts.Runs;
using IntegrationHub.Application.Logging;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;
using IntegrationHub.Infrastructure.Logging;
using IntegrationHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.Api.Controllers;

[ApiController]
[Route("api/integrations/{integrationId:guid}/runs")]
public class RunsController : ControllerBase
{
    private readonly IntegrationHubDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RunLogService _logService;
    
    public RunsController(IntegrationHubDbContext db, IHttpClientFactory httpClientFactory,  RunLogService logService)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logService = logService;
    }

    [HttpPost]
    public async Task<ActionResult> Run(Guid integrationId, CancellationToken ct)
    {
        var integration = await _db.Integrations
            .Include(i => i.SourceEndpoint)
            .Include(i => i.TargetEndpoint)
            .FirstOrDefaultAsync(c => c.Id == integrationId, ct);
        
        if (integration == null) return NotFound("Integration not found");
        if (!integration.IsActive) return BadRequest("Integration not active");
        if(!integration.SourceEndpoint.IsActive)  return BadRequest("SourceEndpoint not active");
        if(!integration.TargetEndpoint.IsActive)  return BadRequest("TargetEndpoint not active");

        var run = new IntegrationRun
        {
            IntegrationId = integration.Id,
            Status = RunStatus.Pending,
            StartedAt = DateTimeOffset.UtcNow,
        };

        
        _db.IntegrationRuns.Add(run);
        await _db.SaveChangesAsync(ct);
        
        var http = _httpClientFactory.CreateClient("integration-client");
        
        var buffer = new RunLogBuffer(run.Id);
        
        try
        {
            buffer.Info("Run Started");

            var sourceUrl = $"{integration.SourceEndpoint.BaseUrl.TrimEnd('/')}/api/orders";
            
            buffer.Info($"Fetching orders from: {sourceUrl} ");
            
            var orders = await http.GetFromJsonAsync<List<OrderDto>>(sourceUrl, ct);
            orders??= new List<OrderDto>();
            
            buffer.Info($"Fetched {orders.Count} orders ");
            
            var targetUrl = $"{integration.TargetEndpoint.BaseUrl.TrimEnd('/')}/api/orders";

            buffer.Info($"Sending orders to: {targetUrl} ");
            
            var response = await http.PostAsJsonAsync(targetUrl, orders,ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                buffer.Error($"Target returned {(int)response.StatusCode}: {responseBody}");
                throw new InvalidOperationException($"Target returned {(int)response.StatusCode}"); 
            }
            
            buffer.Info($"Target OK: {responseBody}");
            
            run.Status = RunStatus.Success;
            run.FinishedAt = DateTimeOffset.UtcNow;
            
            buffer.Info("Run Finished successfully");
            await _db.SaveChangesAsync(ct);
            
            await _logService.AppendAsync(buffer.Logs, ct);
            
            return CreatedAtAction(nameof(GetRun), new { runId = run.Id }, new { runId = run.Id, status = run.Status.ToString() });
        } catch  (Exception ex)
        {
            run.Status = RunStatus.Failed;
            run.FinishedAt = DateTimeOffset.UtcNow;
            run.ErrorMessage = ex.Message;
            
            await _db.SaveChangesAsync(ct);
            
            buffer.Error($"Run failed: {ex.Message}");
            await _logService.AppendAsync(buffer.Logs, ct);
            
            return StatusCode(500, new { runId = run.Id, error = ex.Message });
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