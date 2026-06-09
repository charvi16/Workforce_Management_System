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

    [Required]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    public bool Status { get; set; } = true;

    [MaxLength(50)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public Employee Employee { get; set; } = null!;

    public Project Project { get; set; } = null!;
}
