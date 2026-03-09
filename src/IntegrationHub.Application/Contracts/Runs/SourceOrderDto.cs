namespace IntegrationHub.Application.Contracts.Runs;

public sealed record SourceOrderDto(
    int OrderId,
    string CustomerName,
    decimal Total,
    DateTimeOffset CreatedAt
    );