# Workforce Management System

Enterprise full-stack Workforce Management System built with ASP.NET Core Web API, Angular, SQL Server, and Docker.

## Current Setup Scope

1. Repo and folder structure.
2. .NET clean architecture solution.
3. Angular frontend scaffold.
4. Project documentation.
5. SQL Server connection using Docker.
6. Backend Swagger startup.
7. Angular home page startup.

## Backend

```bash
dotnet restore
dotnet build WMS-Solution.sln
dotnet run --project Backend/WMS.API/WMS.API.csproj
```

Swagger runs in development at:

```text
https://localhost:5001/swagger
http://localhost:5000/swagger
```

## Database

```bash
docker compose up -d sqlserver
```

The development connection string is configured in `Backend/WMS.API/appsettings.Development.json`.

## Frontend

```bash
cd WMS.Frontend
npm install
npm start
```

Angular runs at:

```text
http://localhost:4200
```

## Branch Workflow

- `main`: final stable code.
- `dev`: current integrated working code.
- `feature/*`: individual feature modules.

Codex will not push changes. When a checkpoint is ready, push the branch manually.
