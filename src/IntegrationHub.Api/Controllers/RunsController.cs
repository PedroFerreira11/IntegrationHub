using IntegrationHub.Api.Contracts.Runs;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;
using IntegrationHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogLevel = IntegrationHub.Domain.Enums.LogLevel;

namespace IntegrationHub.Api.Controllers;

[ApiController]
[Route("api/integrations/{integrationId:guid}/runs")]
public class RunsController : ControllerBase
{
    private readonly IntegrationHubDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    
    public RunsController(IntegrationHubDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
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
        
        async Task AddLog(LogLevel level, string message)
        {
            _db.IntegrationLogs.Add(new IntegrationLog
            {
                IntegrationRunId = run.Id,
                Timestamp = DateTimeOffset.UtcNow,
                Level = level,
                Message = message
            });

            await _db.SaveChangesAsync(ct);
        }

        try
        {
            await AddLog(LogLevel.Info, "Run Started");
            
            var http = _httpClientFactory.CreateClient();

            var sourceUrl = $"{integration.SourceEndpoint.BaseUrl.TrimEnd('/')}/api/orders";
            await AddLog(LogLevel.Info, $"Fetching orders from: {sourceUrl} ");
            
            var orders = await http.GetFromJsonAsync<List<OrderDto>>(sourceUrl, ct);
            orders??= new List<OrderDto>();
            
            await AddLog(LogLevel.Info, $"Fetched {orders.Count} orders ");
            
            var targetUrl = $"{integration.TargetEndpoint.BaseUrl.TrimEnd('/')}/api/orders";
            await AddLog(LogLevel.Info, $"Sending orders to: {targetUrl} ");
            
            var response = await http.PostAsJsonAsync(targetUrl, orders,ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                await AddLog(LogLevel.Error, $"Failed to send orders to: {targetUrl} ");
                throw new InvalidOperationException("Failed to send orders to: " + targetUrl); 
            }
            
            await AddLog(LogLevel.Info, $"Successfully Sent orders to: {targetUrl} ");
            
            run.Status = RunStatus.Success;
            run.FinishedAt = DateTimeOffset.UtcNow;
            
            await AddLog(LogLevel.Info, "Run Finished successfully");
            await _db.SaveChangesAsync(ct);
            
            return CreatedAtAction(nameof(GetRun), new { runId = run.Id }, new { runId = run.Id, status = run.Status.ToString() });
        } catch  (Exception ex)
        {
            run.Status = RunStatus.Failed;
            run.FinishedAt = DateTimeOffset.UtcNow;
            run.ErrorMessage = ex.Message;
            
            await _db.SaveChangesAsync(ct);
            await AddLog(LogLevel.Error, $"Run failed: {ex.Message}");
            
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

    [HttpGet("api/runs/{runId:guid}/logs")]
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