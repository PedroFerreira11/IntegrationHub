using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;

namespace IntegrationHub.Application.Logging;

public sealed class RunLogBuffer
{
    private readonly Guid _runId;
    private readonly List<IntegrationLog> _logs = new();

    public RunLogBuffer(Guid runId) => _runId = runId;

    public void Info(string message) => Add(LogLevel.Info, message);
    public void Warning(string message) => Add(LogLevel.Warning, message);
    public void Error(string message) => Add(LogLevel.Error, message);

    private void Add(LogLevel level, string message)
    {
        _logs.Add(new IntegrationLog
        {
            IntegrationRunId = _runId,
            Timestamp = DateTimeOffset.UtcNow,
            Level = level,
            Message = message
        });
    }

    public IReadOnlyList<IntegrationLog> Logs => _logs;
}