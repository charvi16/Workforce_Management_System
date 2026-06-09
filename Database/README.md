# WMS SQL Server Docker Setup

The local database runs from the official Microsoft SQL Server 2022 Docker image.

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
