using System.ComponentModel.DataAnnotations;
using IntegrationHub.Domain.Enums;
using IntegrationHub.Web.Models.Endpoints;

namespace IntegrationHub.Web.Models.Integrations;

public class CreateIntegrationViewModel
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public IntegrationType Type { get; set; }

    [Required]
    public Guid SourceEndpointId { get; set; }

    [Required]
    public Guid TargetEndpointId { get; set; }

    public bool IsActive { get; set; } = true;

    public List<EndpointDto> Endpoints { get; set; } = new();
}