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
            .Select(e => new EndpointResponse(e.Id, e.Name, e.BaseUrl, e.IsActive)).ToListAsync(ct);
        
        return Ok(endpoints);
    }

    [HttpPost]
    public async Task<ActionResult<EndpointResponse>> Create(CreateEndpointRequest request, CancellationToken ct)
    {
        var entity = new SystemEndpoint
        {
            Name = request.Name,
            BaseUrl =  request.BaseUrl,
            ApiKey =  request.ApiKey,
            IsActive =  request.IsActive
        };
        
        _db.SystemEndpoints.Add(entity);
        await _db.SaveChangesAsync(ct);
        
        var response = new EndpointResponse(entity.Id, entity.Name, entity.BaseUrl, entity.IsActive);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EndpointResponse>> GetById(Guid id, CancellationToken ct)
    {
        var e = await _db.SystemEndpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        if (e is null) return NotFound();
        
        return Ok(new EndpointResponse(e.Id, e.Name, e.BaseUrl, e.IsActive));
    }
}