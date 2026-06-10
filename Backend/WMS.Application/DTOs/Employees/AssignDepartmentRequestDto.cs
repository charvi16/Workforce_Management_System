using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs.Employees;

public class AssignDepartmentRequestDto
{
    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }
}
