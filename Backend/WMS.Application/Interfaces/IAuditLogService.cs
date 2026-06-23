using WMS.Application.Common;
using WMS.Application.DTOs.AuditLogs;

namespace WMS.Application.Interfaces;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetAllAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task LogAsync(int? userId, string? username, string action, string? entityName, string? entityId, string? details, string? ipAddress, CancellationToken cancellationToken = default);
}
