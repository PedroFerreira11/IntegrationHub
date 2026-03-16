# IntegrationHub

IntegrationHub is a .NET 8 solution for running asynchronous integrations
between external systems. A client creates an integration run through a REST
API, the run is picked up by a background worker, and the platform executes the
source-to-target sync with retry and logging support.

The project was built to practice backend architecture, background processing,
HTTP integrations, and operational concerns in ASP.NET Core.

## What It Does

- Manage external system endpoints
- Create integrations between source and target systems
- Trigger integration runs through API or Web UI
- Process runs asynchronously with retry support
- Persist runs and logs in SQL Server
- Execute `Orders` and `Customers` flows end-to-end

## Solution Structure

- `src/IntegrationHub.Api`: REST API, Swagger, background worker, run orchestration
- `src/IntegrationHub.Application`: contracts and application abstractions
- `src/IntegrationHub.Domain`: entities and enums
- `src/IntegrationHub.Infrastructure`: EF Core persistence, processors, logging, retry execution
- `src/IntegrationHub.Web`: MVC frontend for endpoints, integrations, and runs
- `services/SourceApi`: mock source system
- `services/TargetApi`: mock target system
- `tests/IntegrationHub.Tests`: automated tests

## Architecture

High-level execution flow:

1. A client creates a run with `POST /api/integrations/{integrationId}/runs`
2. The run is stored with status `Pending`
3. The background worker polls for pending runs
4. `IntegrationRunner` resolves the correct processor for the integration type
5. The processor reads data from the source API and sends it to the target API
6. The run finishes as `Success` or is retried until it becomes `Failed`

Related UML diagrams:

- `docs/uml/ArchitectureDiagram.puml`
- `docs/uml/ClassDiagram.puml`
- `docs/uml/SequenceDiagram.puml`

## Tech Stack

- .NET 8
- ASP.NET Core
- ASP.NET Core MVC
- Entity Framework Core 8
- SQL Server
- BackgroundService
- HttpClientFactory
- xUnit

## Prerequisites

- .NET SDK 8 installed
- SQL Server available locally
- `dotnet-ef` installed globally
- ASP.NET Core development certificate trusted locally

Useful commands:

```powershell
dotnet --version
dotnet tool install --global dotnet-ef
dotnet dev-certs https --trust
```

## Configuration

### Database

The API uses the connection string defined in
`src/IntegrationHub.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=IntegrationHubDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Adjust it if your SQL Server instance is different.

### Local Service URLs

- `IntegrationHub.Api`: `https://localhost:7200`
- `IntegrationHub.Web`: `https://localhost:7201`
- `SourceApi`: `https://localhost:7210`
- `TargetApi`: `https://localhost:7220`

### Demo API Keys

Default values from the mock services:

- `SourceApi`
  - Header: `x-api-key`
  - Value: `source-secret-key`
- `TargetApi`
  - Header: `x-api-key`
  - Value: `target-secret-key`

When creating endpoints in IntegrationHub, use those values.

## Database Setup

Run the EF Core migrations with the API project as startup project:

```powershell
dotnet ef database update --project .\src\IntegrationHub.Infrastructure\IntegrationHub.Infrastructure.csproj --startup-project .\src\IntegrationHub.Api\IntegrationHub.Api.csproj
```

## Running the Project

### Option 1: Start services manually

Run each project with the `https` launch profile:

```powershell
dotnet run --project .\services\SourceApi --launch-profile https
dotnet run --project .\services\TargetApi --launch-profile https
dotnet run --project .\src\IntegrationHub.Api --launch-profile https
dotnet run --project .\src\IntegrationHub.Web --launch-profile https
```

### Option 2: Start all services with the helper script

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\start-dev.ps1
```

## Demo Flow

1. Open `https://localhost:7201`
2. Create a source endpoint:
   - Base URL: `https://localhost:7210`
   - API key header: `x-api-key`
   - API key: `source-secret-key`
3. Create a target endpoint:
   - Base URL: `https://localhost:7220`
   - API key header: `x-api-key`
   - API key: `target-secret-key`
4. Create an integration of type `Orders` or `Customers`
5. Trigger a run from the integration details page
6. Open the run details page and watch status and logs update

The mock services expose both resources under the same base URLs:

- `Orders`
  - Source: `GET /api/orders`
  - Target: `POST /api/orders`
- `Customers`
  - Source: `GET /api/customers`
  - Target: `POST /api/customers`

You can reuse the same source and target endpoints and create multiple
integrations that differ only by `Type`.

## Main API Endpoints

- `GET /api/endpoints`
- `POST /api/endpoints`
- `GET /api/endpoints/{id}`
- `DELETE /api/endpoints/{id}`
- `GET /api/integrations`
- `POST /api/integrations`
- `GET /api/integrations/{id}`
- `DELETE /api/integrations/{id}`
- `POST /api/integrations/{integrationId}/runs`
- `GET /api/runs/{runId}`
- `GET /api/runs/{runId}/logs`

## Running Tests

```powershell
dotnet test .\IntegrationHub.sln
```

CI also runs build and tests through `.github/workflows/ci.yml`.

## Known Limitations

- There is no authentication or authorization in the main API or Web UI
- Endpoint and integration editing is not implemented yet
- Secrets are stored directly in the database for demo purposes
- The setup assumes local SQL Server and local development certificates

## Author

Pedro
