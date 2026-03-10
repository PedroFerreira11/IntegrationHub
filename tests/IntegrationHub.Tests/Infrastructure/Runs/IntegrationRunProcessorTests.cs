using IntegrationHub.Application.Runs;
using IntegrationHub.Domain.Entities;
using IntegrationHub.Domain.Enums;
using IntegrationHub.Infrastructure.Persistence;
using IntegrationHub.Infrastructure.Runs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;



namespace IntegrationHub.Tests.Infrastructure.Runs;

public class IntegrationRunProcessorTests
{
    [Fact]
    public async Task TryProcessNextRunAsync_WhenPendingRunExistsAndRunnerSucceeds_MarksRunAsSuccess()
    {
        var options = CreateOptions();
        
        var runId = Guid.NewGuid();
        var integrationId = Guid.NewGuid();
        
        await using (var db = new IntegrationHubDbContext(options))
        { 
            await SeedRunAsync(
                db,
                integrationId,
                runId,
                RunStatus.Pending,
                0,
                3,
                null);
        }

        var fakeRunner = new FakeIntegrationRunner();

        var processed = await ExecuteProcessor(options, fakeRunner);
        Assert.True(processed);
        Assert.Equal(runId, fakeRunner.ReceivedRunId);

        await using (var db = new IntegrationHubDbContext(options))
        {
            var run = await db.IntegrationRuns.SingleAsync(r => r.Id == runId);
            
            Assert.Equal(RunStatus.Success, run.Status);
            Assert.Equal(0, run.RetryCount);
            Assert.NotNull(run.StartedAt);
            Assert.NotNull(run.FinishedAt);
            Assert.Null(run.NextRetryAt);
            Assert.Null(run.ErrorMessage);
        }
    }
    
    [Fact]
    public async Task TryProcessNextRunAsync_WhenRunnerThrowsAndRetryIsAvailable_ReschedulesRun()
    {
        var options = CreateOptions();
        
        var runId = Guid.NewGuid();
        var integrationId = Guid.NewGuid();
        
        await using (var db = new IntegrationHubDbContext(options))
        { 
            await SeedRunAsync(
                db,
                integrationId,
                runId,
                RunStatus.Pending,
                0,
                3,
                null);
        }
        
        var fakeRunner = new FakeFailedIntegrationRunner();
        
        var beforeExecution = DateTimeOffset.UtcNow;
        
        var processed = await ExecuteProcessor(options, fakeRunner);
        Assert.True(processed);
        Assert.Equal(runId, fakeRunner.ReceivedRunId);

        await using (var db = new IntegrationHubDbContext(options))
        {
            var run = await db.IntegrationRuns.SingleAsync(r => r.Id == runId);
            
            Assert.Equal(RunStatus.Pending, run.Status);
            Assert.Equal(1, run.RetryCount);
            Assert.NotNull(run.StartedAt);
            Assert.Null(run.FinishedAt);
            Assert.True(run.NextRetryAt > beforeExecution);
            Assert.Equal("SimulationFailed", run.ErrorMessage);
        }
    }

    [Fact]
    public async Task TryProcessNextRunAsync_WhenRunnerThrowsAndRetryIsNotAvailable_MarksRunAsFailed()
    {
        var options = CreateOptions();
        
        var runId = Guid.NewGuid();
        var integrationId = Guid.NewGuid();

        await using (var db = new IntegrationHubDbContext(options))
        {
            await SeedRunAsync(
                db,
                integrationId,
                runId,
                RunStatus.Pending,
                3,
                3,
                null);
        }
        
        var fakeRunner = new FakeFailedIntegrationRunner();
        
        var processed = await ExecuteProcessor(options, fakeRunner);
        Assert.True(processed);
        Assert.Equal(runId, fakeRunner.ReceivedRunId);

        await using (var db = new IntegrationHubDbContext(options))
        {
            var run = await db.IntegrationRuns.SingleAsync(r => r.Id == runId);
            
            Assert.Equal(RunStatus.Failed, run.Status);
            Assert.Equal(4, run.RetryCount);
            Assert.NotNull(run.StartedAt);
            Assert.NotNull(run.FinishedAt);
            Assert.Null(run.NextRetryAt);
            Assert.Equal("SimulationFailed", run.ErrorMessage);
        }
    }
    
    [Fact]
    public async Task TryProcessNextRunAsync_WhenNoRunnableRunExists_ReturnsFalse()
    {
        var options = CreateOptions();

        var processed = await ExecuteProcessor(options, new FakeIntegrationRunner());

        Assert.False(processed);
    }
    
    [Fact]
    public async Task TryProcessNextRunAsync_WhenNextRetryAtIsInFuture_DoesNotProcessRun()
    {
        var options = CreateOptions();

        var runId = Guid.NewGuid();
        var integrationId = Guid.NewGuid();

        await using (var db = new IntegrationHubDbContext(options))
        {
            await SeedRunAsync(
                db,
                integrationId,
                runId,
                RunStatus.Pending,
                1,
                3,
                DateTimeOffset.UtcNow.AddMinutes(5));
        }

        var fakeRunner = new FakeIntegrationRunner();

        var processed = await ExecuteProcessor(options, fakeRunner);

        Assert.False(processed);
        Assert.Null(fakeRunner.ReceivedRunId);

        await using (var db = new IntegrationHubDbContext(options))
        {
            var run = await db.IntegrationRuns.SingleAsync(r => r.Id == runId);

            Assert.Equal(RunStatus.Pending, run.Status);
            Assert.Equal(1, run.RetryCount);
            Assert.Null(run.StartedAt);
        }
    }
    
    [Fact]
    public async Task TryProcessNextRunAsync_WhenMultipleRunsExist_ProcessesOldestRunFirst()
    {
        var options = CreateOptions();

        var integrationId = Guid.NewGuid();

        var oldestRunId = Guid.NewGuid();
        var newestRunId = Guid.NewGuid();

        var now = DateTimeOffset.UtcNow;

        await using (var db = new IntegrationHubDbContext(options))
        {
            db.Integrations.Add(new Integration
            {
                Id = integrationId,
                Name = "Orders sync",
                Type = IntegrationType.Orders,
                IsActive = true,
                SourceEndpointId = Guid.NewGuid(),
                TargetEndpointId = Guid.NewGuid()
            });

            db.IntegrationRuns.Add(new IntegrationRun
            {
                Id = oldestRunId,
                IntegrationId = integrationId,
                Status = RunStatus.Pending,
                CreatedAt = now.AddMinutes(-10),
                RetryCount = 0,
                MaxRetries = 3
            });

            db.IntegrationRuns.Add(new IntegrationRun
            {
                Id = newestRunId,
                IntegrationId = integrationId,
                Status = RunStatus.Pending,
                CreatedAt = now,
                RetryCount = 0,
                MaxRetries = 3
            });

            await db.SaveChangesAsync();
        }

        var fakeRunner = new FakeIntegrationRunner();

        var processed = await ExecuteProcessor(options, fakeRunner);

        Assert.True(processed);
        Assert.Equal(oldestRunId, fakeRunner.ReceivedRunId);

        await using (var db = new IntegrationHubDbContext(options))
        {
            var oldestRun = await db.IntegrationRuns.SingleAsync(r => r.Id == oldestRunId);
            var newestRun = await db.IntegrationRuns.SingleAsync(r => r.Id == newestRunId);

            Assert.Equal(RunStatus.Success, oldestRun.Status);
            Assert.Equal(RunStatus.Pending, newestRun.Status);
        }
    }
    
    private sealed class FakeIntegrationRunner : IIntegrationRunner
    {
        public Guid? ReceivedRunId { get; private set; }

        public Task RunAsync(Guid runId, CancellationToken ct)
        {
            ReceivedRunId = runId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFailedIntegrationRunner : IIntegrationRunner
    {
        public Guid? ReceivedRunId { get; private set; }

        public Task RunAsync(Guid runId, CancellationToken ct)
        {
            ReceivedRunId = runId;
            throw new InvalidOperationException("SimulationFailed");
        }
    }
    
    private static DbContextOptions<IntegrationHubDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<IntegrationHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }
    
    private static async Task SeedRunAsync(
        IntegrationHubDbContext db,
        Guid integrationId,
        Guid runId,
        RunStatus status,
        int retryCount = 0,
        int maxRetries = 3,
        DateTimeOffset? nextRetryAt = null)
    {
        db.Integrations.Add(new Integration
        {
            Id = integrationId,
            Name = "Orders sync",
            Type = IntegrationType.Orders,
            IsActive = true,
            SourceEndpointId = Guid.NewGuid(),
            TargetEndpointId = Guid.NewGuid()
        });

        db.IntegrationRuns.Add(new IntegrationRun
        {
            Id = runId,
            IntegrationId = integrationId,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            RetryCount = retryCount,
            MaxRetries = maxRetries,
            NextRetryAt = nextRetryAt
        });

        await db.SaveChangesAsync();
    }
    
    private static async Task<bool> ExecuteProcessor(
        DbContextOptions<IntegrationHubDbContext> options,
        IIntegrationRunner runner)
    {
        await using var db = new IntegrationHubDbContext(options);

        var processor = new IntegrationRunProcessor(
            db,
            runner,
            NullLogger<IntegrationRunProcessor>.Instance
        );

        return await processor.TryProcessNextRunAsync(CancellationToken.None);
    }
}