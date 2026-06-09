# WMS SQL Server Docker Setup

The local database runs from a SQL Server-compatible Docker image. On Apple Silicon, the official SQL Server 2022 AMD64 image can crash under emulation, so this project uses `mcr.microsoft.com/azure-sql-edge:latest` for local development.

```bash
docker compose up -d sqlserver
```

Development connection string:

```text
Server=localhost,1433;Database=WMSDb;User Id=sa;Password=Wms@12345;TrustServerCertificate=True;Encrypt=False
```

The API health endpoint for the database is:

```text
GET /api/v1/health/database
```
