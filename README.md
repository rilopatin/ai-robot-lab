# Opportunity OS

Opportunity OS is a small, goal-driven system for finding real sources of online income through a disciplined learning loop:

```text
Goal → Opportunity → Experiment → Evidence → Decision
```

The v0.2A application focuses on persistent system memory. It records zero-cost experiments, append-only evidence, and explicit `GO`, `WATCH`, `KILL`, `PIVOT`, or `SCALE` decisions. It does not automate research or run AI agents.

## Requirements

- .NET 10 SDK
- Node.js and npm

## Projects

- `src/api` — ASP.NET Core Minimal API using EF Core and SQLite
- `src/web` — React + TypeScript application using Vite
- `tests/OpportunityOs.Api.Tests` — API integration tests using temporary SQLite databases

## Build and test

```powershell
dotnet build OpportunityOs.slnx
dotnet test OpportunityOs.slnx

Set-Location src/web
npm.cmd install
npm.cmd run build
```

## Run locally

Start the API:

```powershell
dotnet run --project src/api/OpportunityOs.Api.csproj
```

In another terminal, start the frontend:

```powershell
Set-Location src/web
npm.cmd run dev
```

Open the URL printed by Vite. The frontend continues to proxy `/api` requests to `http://localhost:5084`.

Application data is stored locally in `src/api/App_Data/opportunity-os.db`. The API applies committed EF Core migrations at startup, and the database is retained across application restarts. Local database files are not committed to Git.
