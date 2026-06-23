namespace WMS.Application.DTOs.Employees;

public class UpdateEmployeeRequestDto
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress, System.ComponentModel.DataAnnotations.MaxLength(80)]
    public string Email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(1, 3)]
    public int Gender { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public DateTime DOB { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public DateTime DOJ { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int RoleId { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, 2)]
    public int Status { get; set; } = 1;
}
