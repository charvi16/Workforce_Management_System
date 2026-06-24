using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs.Projects;

public class ProjectRequestDto
{
    [Required, MaxLength(100)]
    public string ProjectName { get; set; } = string.Empty;

    public int? ClientId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Planned";

    public IReadOnlyCollection<int> MemberIds { get; set; } = Array.Empty<int>();
}
