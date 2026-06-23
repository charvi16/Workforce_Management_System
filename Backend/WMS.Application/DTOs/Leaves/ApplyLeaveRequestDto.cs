using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs.Leaves;

public class ApplyLeaveRequestDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int LeaveType { get; set; }

    [MaxLength(255)]
    public string? Reason { get; set; }

    [Required]
    public DateTime FromDate { get; set; }

    [Required]
    public DateTime ToDate { get; set; }
}
