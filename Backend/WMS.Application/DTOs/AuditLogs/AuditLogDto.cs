namespace WMS.Application.DTOs.AuditLogs;

public class AuditLogDto
{
    public int AuditLogId { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? IpAddress { get; set; }
}
