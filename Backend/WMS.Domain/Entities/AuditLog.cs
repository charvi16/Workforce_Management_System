using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.Entities;

public class AuditLog
{
    [Key]
    public int AuditId { get; set; }

    public int? UserId { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? EntityId { get; set; }

    public string? Details { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? IpAddress { get; set; }
}
