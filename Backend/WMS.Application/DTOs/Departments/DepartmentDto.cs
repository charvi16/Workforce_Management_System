namespace WMS.Application.DTOs.Departments;

public class DepartmentDto
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int EmployeeCount { get; set; }
}
