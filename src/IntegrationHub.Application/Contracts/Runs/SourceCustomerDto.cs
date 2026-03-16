namespace IntegrationHub.Application.Contracts.Runs;

public sealed record SourceCustomerDto(
    int CustomerId,
    string Name,
    string Email,
    DateTimeOffset CreatedAt
);
