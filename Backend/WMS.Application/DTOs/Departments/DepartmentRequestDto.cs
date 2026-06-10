using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs.Departments;

public class DepartmentRequestDto
{
    [Required, MaxLength(100)]
    public string DepartmentName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }
}
