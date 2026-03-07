namespace IntegrationHub.Application.Runs;

public sealed record RunResult(Guid IntegrationId, string Status);