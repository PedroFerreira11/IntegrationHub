namespace IntegrationHub.Application.Contracts.Runs;

public sealed record TargetOrderDto(
    int OrderId,
    string Customer,
    decimal Total
    );