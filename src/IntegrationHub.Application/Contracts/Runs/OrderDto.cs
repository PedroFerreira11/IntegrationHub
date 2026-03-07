namespace IntegrationHub.Application.Contracts.Runs;

public sealed record OrderDto(
    int OrderId,
    string CustomerName,
    decimal Total,
    DateTimeOffset CreatedAt
);