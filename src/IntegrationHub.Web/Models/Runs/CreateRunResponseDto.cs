namespace IntegrationHub.Web.Models.Runs;

public record CreateRunResponseDto(
    Guid Id,
    Guid IntegrationId,
    string Status,
    DateTimeOffset CreatedAt,
    int RetryCount,
    int MaxRetries
);