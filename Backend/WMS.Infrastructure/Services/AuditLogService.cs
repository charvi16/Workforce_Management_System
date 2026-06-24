using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.AuditLogs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly WmsDbContext _dbContext;

    public AuditLogService(WmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AuditLogDto>> GetAllAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        await EnsureAuditLogSchemaAsync(cancellationToken);

        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.AuditLogs.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                AuditLogId = a.AuditId,
                UserId = a.UserId,
                Username = a.Username,
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Details = a.Details,
                CreatedOn = a.CreatedOn,
                IpAddress = a.IpAddress
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task LogAsync(int? userId, string? username, string action, string? entityName, string? entityId, string? details, string? ipAddress, CancellationToken cancellationToken = default)
    {
        await EnsureAuditLogSchemaAsync(cancellationToken);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Username = username,
            Action = action,
            EntityName = entityName ?? string.Empty,
            EntityId = entityId,
            Details = details,
            CreatedOn = DateTime.UtcNow,
            IpAddress = ipAddress
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task EnsureAuditLogSchemaAsync(CancellationToken cancellationToken)
    {
        if (!_dbContext.Database.IsSqlServer())
        {
            return Task.CompletedTask;
        }

        return _dbContext.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.AuditLogs', 'UserId') IS NULL
    BEGIN
        ALTER TABLE dbo.AuditLogs ADD UserId int NULL;
        IF COL_LENGTH('dbo.AuditLogs', 'CreatedBy') IS NOT NULL
        BEGIN
            EXEC('UPDATE dbo.AuditLogs SET UserId = CreatedBy WHERE UserId IS NULL');
        END
    END

    IF COL_LENGTH('dbo.AuditLogs', 'Username') IS NULL
    BEGIN
        ALTER TABLE dbo.AuditLogs ADD Username varchar(100) NULL;
    END

    IF COL_LENGTH('dbo.AuditLogs', 'EntityId') IS NULL
    BEGIN
        ALTER TABLE dbo.AuditLogs ADD EntityId varchar(50) NULL;
        IF COL_LENGTH('dbo.AuditLogs', 'RecordId') IS NOT NULL
        BEGIN
            EXEC('UPDATE dbo.AuditLogs SET EntityId = CONVERT(varchar(50), RecordId) WHERE EntityId IS NULL');
        END
    END

    IF COL_LENGTH('dbo.AuditLogs', 'Details') IS NULL
    BEGIN
        ALTER TABLE dbo.AuditLogs ADD Details varchar(max) NULL;
    END

    IF COL_LENGTH('dbo.AuditLogs', 'IpAddress') IS NULL
    BEGIN
        ALTER TABLE dbo.AuditLogs ADD IpAddress varchar(50) NULL;
    END

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID('dbo.AuditLogs')
          AND c.name = 'Action'
          AND c.max_length > 0
          AND c.max_length < 100
    )
    BEGIN
        ALTER TABLE dbo.AuditLogs ALTER COLUMN Action varchar(100) NOT NULL;
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_CreatedOn' AND object_id = OBJECT_ID('dbo.AuditLogs'))
    BEGIN
        CREATE INDEX IX_AuditLogs_CreatedOn ON dbo.AuditLogs (CreatedOn);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_UserId' AND object_id = OBJECT_ID('dbo.AuditLogs'))
    BEGIN
        CREATE INDEX IX_AuditLogs_UserId ON dbo.AuditLogs (UserId);
    END
END
""", cancellationToken);
    }
}
