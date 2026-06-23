using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.Entities;

public class EmployeeProjectAllocation
{
    [Key]
    public int AllocationId { get; set; }

    public int EmpId { get; set; }

    public int ProjectId { get; set; }

    [Required]
    public DateTime AssignedOn { get; set; }

    [MaxLength(50)]
    public string? RoleInProject { get; set; }

    public int? AllocationPercentage { get; set; }

    public bool Status { get; set; } = true;

    public DateTime CreatedOn { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public Employee Employee { get; set; } = null!;

    public Project Project { get; set; } = null!;
}
