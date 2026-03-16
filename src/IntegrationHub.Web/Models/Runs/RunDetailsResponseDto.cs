namespace IntegrationHub.Web.Models.Runs;

public record RunDetailsResponseDto(
    Guid Id,
    Guid IntegrationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string Status,
    string? ErrorMessage,
    int RetryCount,
    int MaxRetries,
    DateTimeOffset? NextRetryAt
);