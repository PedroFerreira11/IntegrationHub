using IntegrationHub.Api.Contracts.Integrations;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.Api.Controllers;

[ApiController]
[Route("api/integrations")]
public class IntegrationsController : ControllerBase
{
    private readonly IntegrationHubDbContext _db;
    
    public IntegrationsController(IntegrationHubDbContext db)
    {
    _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<IntegrationResponse>>> GetAll(CancellationToken ct)
    {
        var integrations = await _db.Integrations
            .AsNoTracking()
            .Include(i => i.SourceEndpoint)
            .Include(i => i.TargetEndpoint)
            .OrderBy(i => i.Name)
            .Select(i=> new IntegrationResponse(
                i.Id,
                i.Name,
                i.Type,
                i.SourceEndpointId,
                i.SourceEndpoint.Name,
                i.TargetEndpointId,
                i.TargetEndpoint.Name,
                i.IsActive
                ))
            .ToListAsync(ct);
        
        return Ok(integrations);
    }

    [HttpPost]
    public async Task<ActionResult<IntegrationResponse>> Create(CreateIntegrationRequest request, CancellationToken ct)
    {
        if (request.SourceEndpointId == request.TargetEndpointId)
            return BadRequest("Source and Target can't be the same endpoint!");

        var source = await _db.SystemEndpoints.FirstOrDefaultAsync(e => e.Id == request.SourceEndpointId, ct);
        if (source is null) return BadRequest("SourceEndpoint doesn't exist!");
        
        var target = await _db.SystemEndpoints.FirstOrDefaultAsync(e => e.Id == request.TargetEndpointId, ct);
        if (target is null) return BadRequest("TargetEndpoint doesn't exist!");

        var entity = new Integration
        {
            Name = request.Name,
            Type = request.Type,
            SourceEndpointId = source.Id,
            TargetEndpointId = target.Id,
            IsActive = request.IsActive
        };
        
        _db.Integrations.Add(entity);
        await _db.SaveChangesAsync(ct);
        
        var response = new IntegrationResponse(
            entity.Id,
            entity.Name,
            entity.Type,
            source.Id,
            source.Name,
            target.Id,
            target.Name,
            entity.IsActive
        );
        
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IntegrationResponse>> GetById(Guid id, CancellationToken ct)
    {
        var integration = await _db.Integrations
            .AsNoTracking()
            .Include(i => i.SourceEndpoint)
            .Include(i => i.TargetEndpoint)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        if (integration is null) return NotFound();

        var response = new IntegrationResponse(
            integration.Id,
            integration.Name,
            integration.Type,
            integration.SourceEndpointId,
            integration.SourceEndpoint.Name,
            integration.TargetEndpointId,
            integration.TargetEndpoint.Name,
            integration.IsActive
            );
        
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var integration = await _db.Integrations.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (integration is null) 
            return NotFound();

        _db.Integrations.Remove(integration);
        await _db.SaveChangesAsync(ct);
        
        return NoContent();
    }
}