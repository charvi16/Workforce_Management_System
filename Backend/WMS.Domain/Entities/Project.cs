using System.ComponentModel.DataAnnotations;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Project
{
    [Key]
    public int ProjectId { get; set; }

    [Required, MaxLength(100)]
    public string ProjectName { get; set; } = string.Empty;

    public int? ClientId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    public Client? Client { get; set; }

    public ICollection<EmployeeProjectAllocation> EmployeeAllocations { get; set; } = new List<EmployeeProjectAllocation>();
}
