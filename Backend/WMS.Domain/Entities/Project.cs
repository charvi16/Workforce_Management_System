using System.ComponentModel.DataAnnotations;
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

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Planned";

    public Client? Client { get; set; }

    public ICollection<EmployeeProjectAllocation> EmployeeAllocations { get; set; } = new List<EmployeeProjectAllocation>();
}
