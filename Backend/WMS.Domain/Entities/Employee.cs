using System.ComponentModel.DataAnnotations;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Employee
{
    [Key]
    public int EmployeeId { get; set; }

    [Required, MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(80)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public Gender Gender { get; set; }

    [Required]
    public DateTime DOB { get; set; }

    [Required]
    public DateTime DOJ { get; set; }

    public int DepartmentId { get; set; }

    public int RoleId { get; set; }

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedOn { get; set; }

    public Department Department { get; set; } = null!;

    public Role Role { get; set; } = null!;

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public ICollection<Leave> Leaves { get; set; } = new List<Leave>();

    public ICollection<EmployeeProjectAllocation> ProjectAllocations { get; set; } = new List<EmployeeProjectAllocation>();

    public UserLogin? UserLogin { get; set; }
}
