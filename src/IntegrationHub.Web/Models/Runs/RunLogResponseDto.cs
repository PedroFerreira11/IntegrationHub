namespace IntegrationHub.Web.Models.Runs;

public record RunLogResponseDto(
    DateTimeOffset Timestamp,
    string Level,
    string Message
    );