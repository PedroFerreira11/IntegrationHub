using IntegrationHub.Domain.Entities;

namespace IntegrationHub.Application.Runs;

public static class IntegrationRetryPolicy
{
    public static bool CanRetry(IntegrationRun run)
    {
        return run.RetryCount <= run.MaxRetries;
    }

    public static TimeSpan GetRetryDelay(int retryCount)
    {
        return retryCount switch
        {
            1 => TimeSpan.FromSeconds(10),
            2 => TimeSpan.FromSeconds(30),
            3 => TimeSpan.FromMinutes(1),
            _ => TimeSpan.FromMinutes(2)
        };
    }
}
