using IntegrationHub.Api.Background;
using IntegrationHub.Application.Processors;
using IntegrationHub.Application.Runs;
using IntegrationHub.Infrastructure.Logging;
using IntegrationHub.Infrastructure.Persistence;
using IntegrationHub.Infrastructure.Processors;
using IntegrationHub.Infrastructure.Runs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<IntegrationHubDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient("integration-client").AddStandardResilienceHandler();

builder.Services.AddScoped<RunLogService>();
builder.Services.AddScoped<IIntegrationRunner, IntegrationRunner>();
builder.Services.AddHostedService<IntegrationRunWorker>();
builder.Services.AddScoped<IIntegrationProcessorResolver, IntegrationProcessorResolver>();
builder.Services.AddScoped<IIntegrationProcessor, OrdersIntegrationProcessor>();
builder.Services.AddScoped<IIntegrationProcessor, CustomersIntegrationProcessor>();

builder.Services.AddScoped<IIntegrationRunner, IntegrationRunner>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();