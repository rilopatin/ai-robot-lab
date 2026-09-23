# AI Robot Lab

AI Robot Lab is a learning project built with an ASP.NET Core API and a React + TypeScript frontend.

## Requirements

- .NET 10 SDK
- Node.js and npm

## Projects

- `src/api` - ASP.NET Core Web API
- `src/web` - React + TypeScript application using Vite

## Build

```powershell
dotnet build

Set-Location src/web
npm.cmd install
npm.cmd run build
```

## Run locally

Start the API:

```powershell
dotnet run --project src/api
```

In another terminal, start the frontend:

```powershell
Set-Location src/web
npm.cmd run dev
```

Open the URL printed by Vite. Projects and experiments are stored in memory and are cleared when the API stops.
