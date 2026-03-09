namespace IntegrationHub.Application.Contracts.Runs;

public sealed record TargetOrderDto(
    int Id,
    string Client,
    decimal Amount
    );