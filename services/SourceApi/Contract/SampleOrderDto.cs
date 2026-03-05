namespace SourceApi.Contract;

public sealed record SampleOrderDto(
    int OrderId,
    string CustomerName,
    decimal Total,
    DateTimeOffset CreatedAt
    );