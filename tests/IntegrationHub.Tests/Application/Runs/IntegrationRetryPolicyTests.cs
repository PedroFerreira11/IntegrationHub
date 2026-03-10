using IntegrationHub.Application.Runs;
using IntegrationHub.Domain.Entities;

namespace IntegrationHub.Tests.Application.Runs;

public class IntegrationRetryPolicyTests
{
    [Theory]
    [InlineData(1,10)]
    [InlineData(2,30)]
    [InlineData(3,60)]
    [InlineData(4,120)]
    public void GetRetryDelay_WhenRetryCountChanges_ReturnsExpectedDelay(int retryCount, int expectedSeconds)
    {
        var expectedDelay = TimeSpan.FromSeconds(expectedSeconds);

        var actualDelay = IntegrationRetryPolicy.GetRetryDelay(retryCount);
        
        Assert.Equal(expectedDelay, actualDelay);
    }

    [Fact]
    public void CanRetry_WhenRetryCountIsEqualToMaxRetries_ReturnsTrue()
    {
        var run = new IntegrationRun
        {
            RetryCount = 3,
            MaxRetries = 3
        };
        
        Assert.True(IntegrationRetryPolicy.CanRetry(run));
    }
    
    [Fact]
    public void CanRetry_WhenRetryCountIsGreaterThanMaxRetries_ReturnsFalse()
    {
        var run = new IntegrationRun
        {
            RetryCount = 4,
            MaxRetries = 3
        };
        
        Assert.False(IntegrationRetryPolicy.CanRetry(run));
    }
}
