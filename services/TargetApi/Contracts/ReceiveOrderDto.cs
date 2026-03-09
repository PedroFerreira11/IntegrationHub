namespace TargetApi.Contracts;

public sealed record ReceiveOrderDto(
    int OrderId,
    string Customer,
    decimal Total
    );