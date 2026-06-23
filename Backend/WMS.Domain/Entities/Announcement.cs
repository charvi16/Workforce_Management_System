using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.Entities;

public class Announcement
{
    [Key]
    public int AnnouncementId { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public int CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedOn { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(20)]
    public string? TargetRole { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public Employee Creator { get; set; } = null!;
}
