using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs.Attendance;

public class CheckOutRequestDto
{
    [Required]
    public int EmployeeId { get; set; }
}
