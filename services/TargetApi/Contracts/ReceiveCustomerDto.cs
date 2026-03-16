namespace TargetApi.Contracts;

public sealed record ReceiveCustomerDto(
    int CustomerId,
    string FullName,
    string Email
);
