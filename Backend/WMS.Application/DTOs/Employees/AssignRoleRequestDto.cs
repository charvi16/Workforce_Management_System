using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs.Employees;

public class AssignRoleRequestDto
{
    [Range(1, int.MaxValue)]
    public int RoleId { get; set; }
}
