using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.Entities;

public class Announcement
{
    [Key]
    public int AnnouncementId { get; set; }

    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public int CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public Employee Creator { get; set; } = null!;
}
