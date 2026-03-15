using System.ComponentModel.DataAnnotations;

namespace IntegrationHub.Web.Models.Endpoints;

public class CreateEndpointDto
{
    [Required]
    public string Name  { get; set; } = string.Empty;
    [Required]
    public string BaseUrl   { get; set; } =  string.Empty;
    
    public string? ApiKey  { get; set; }
    public string? ApiKeyHeaderName  { get; set; }
    public bool IsActive  { get; set; } =  true;
}