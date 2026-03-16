namespace IntegrationHub.Application.Contracts.Runs;

public sealed record TargetCustomerDto(
    int CustomerId,
    string FullName,
    string Email
);
