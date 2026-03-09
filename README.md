# IntegrationHub

IntegrationHub is a **.NET 8 backend service** that executes integrations between external systems.

Integrations are triggered through a REST API and executed asynchronously by a background worker, which handles execution, retries, and logging.

This project was built to practice **backend architecture, background processing, and resilient integrations in .NET**.

---

## Features

- Trigger integrations via REST API
- Asynchronous execution using a background worker
- Retry mechanism for failed integrations
- Execution status tracking
- Structured logging for integration runs
- HTTP integrations using `HttpClientFactory`

---

## Architecture

The project follows a layered architecture:

- **API** – exposes REST endpoints and hosts background workers
- **Application** – application services and contracts
- **Domain** – core domain entities and enums
- **Infrastructure** – database access, integration execution and logging

---

## Execution Flow

1. Client triggers an integration run  
   `POST /api/integrations/{integrationId}/runs`

2. A run is created with status **Pending**

3. The background worker picks up pending runs

4. `IntegrationRunner` executes the integration:
    - Fetch data from **source API**
    - Send data to **target API**

5. Run status is updated:
    - `Success`
    - `Failed` (after retry limit)

---

## Technologies

- .NET 8
- ASP.NET Core
- Entity Framework Core
- SQL Server
- BackgroundService
- HttpClientFactory

---

## Running the Project

```bash
git clone https://github.com/yourusername/IntegrationHub.git
cd IntegrationHub
dotnet ef database update
dotnet run --project src/SourceApi &
dotnet run --project src/TargetApi &
dotnet run --project src/IntegrationHub.Api
```

---

## Author 
Pedro