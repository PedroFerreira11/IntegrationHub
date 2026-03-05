namespace TargetApi.Contracts;

public sealed record ReceiveOrderDto(
    int OrderId,
    string CustomerName,
    decimal Total,
    DateTimeOffset CreatedAt
    );