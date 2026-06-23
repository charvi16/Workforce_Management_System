using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs.ProjectAllocations;

public class ProjectAllocationRequestDto
{
    [Range(1, int.MaxValue)]
    public int EmpId { get; set; }

    [Range(1, int.MaxValue)]
    public int ProjectId { get; set; }

    [Required]
    public DateTime AssignedOn { get; set; }

    [MaxLength(50)]
    public string? RoleInProject { get; set; }

    [Range(1, 100)]
    public int? AllocationPercentage { get; set; }

    public bool Status { get; set; } = true;
}
