namespace SourceApi.Contract;

public sealed record SampleCustomerDto(
    int CustomerId,
    string Name,
    string Email,
    DateTimeOffset CreatedAt
);
