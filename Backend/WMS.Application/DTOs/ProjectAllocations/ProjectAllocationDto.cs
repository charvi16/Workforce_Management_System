namespace WMS.Application.DTOs.ProjectAllocations;

public class ProjectAllocationDto
{
    public int AllocationId { get; set; }

    public int EmpId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string? ClientName { get; set; }

    public DateTime AssignedOn { get; set; }

    public string? RoleInProject { get; set; }

    public int? AllocationPercentage { get; set; }

    public bool Status { get; set; }

    public DateTime CreatedOn { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }
}
