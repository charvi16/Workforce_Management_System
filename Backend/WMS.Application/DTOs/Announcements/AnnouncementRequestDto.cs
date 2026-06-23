using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs.Announcements;

public class AnnouncementRequestDto
{
    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? TargetRole { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public bool IsActive { get; set; } = true;
}
