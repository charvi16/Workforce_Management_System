using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs.Projects;

public class ProjectStatusUpdateDto
{
    [Required, MaxLength(20)]
    public string Status { get; set; } = string.Empty;
}
