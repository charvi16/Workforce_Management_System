using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs.Attendance;

public class CheckInRequestDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int WorkMode { get; set; }
}
