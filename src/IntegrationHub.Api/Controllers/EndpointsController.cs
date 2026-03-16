using IntegrationHub.Api.Contracts.Endpoints;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationHub.Api.Controllers;


[ApiController]
[Route("api/endpoints")]
public class EndpointsController : ControllerBase
{
    private readonly IntegrationHubDbContext _db;
    
    public EndpointsController(IntegrationHubDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<EndpointResponse>>> GetAll(CancellationToken ct)
    {
        var endpoints = await _db.SystemEndpoints
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new EndpointResponse(
                e.Id,
                e.Name,
                e.BaseUrl,
                !string.IsNullOrWhiteSpace(e.ApiKey),
                e.ApiKeyHeaderName,
                e.IsActive))
            .ToListAsync(ct);

        return Ok(endpoints);
    }

    [HttpPost]
    public async Task<ActionResult<EndpointResponse>> Create(CreateEndpointRequest request, CancellationToken ct)
    {
        var hasApiKey = !string.IsNullOrWhiteSpace(request.ApiKey);
        var hasHeaderName = !string.IsNullOrWhiteSpace(request.ApiKeyHeaderName);

        if (hasApiKey != hasHeaderName)
        {
            return BadRequest("ApiKey e ApiKeyHeaderName can't be null or empty.");
        }
        
        var entity = new SystemEndpoint
        {
            Name = request.Name,
            BaseUrl = request.BaseUrl.TrimEnd('/'),
            ApiKey = request.ApiKey,
            ApiKeyHeaderName = request.ApiKeyHeaderName,
            IsActive = request.IsActive
        };

        _db.SystemEndpoints.Add(entity);
        await _db.SaveChangesAsync(ct);

        var response = new EndpointResponse(
            entity.Id,
            entity.Name,
            entity.BaseUrl,
            !string.IsNullOrWhiteSpace(entity.ApiKey),
            entity.ApiKeyHeaderName,
            entity.IsActive);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EndpointResponse>> GetById(Guid id, CancellationToken ct)
    {
        var e = await _db.SystemEndpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (e is null)
            return NotFound();

        return Ok(new EndpointResponse(
            e.Id,
            e.Name,
            e.BaseUrl,
            !string.IsNullOrWhiteSpace(e.ApiKey),
            e.ApiKeyHeaderName,
            e.IsActive));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var endpoint = await _db.SystemEndpoints.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (endpoint is null)
            return NotFound();

        var isInUse = await _db.Integrations.AnyAsync(
            i => i.SourceEndpointId == id || i.TargetEndpointId == id, ct);

        if (isInUse)
            return BadRequest("Endpoint cannot be deleted because it is used by an integration.");

        _db.SystemEndpoints.Remove(endpoint);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}