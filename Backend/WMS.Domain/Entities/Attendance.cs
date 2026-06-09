using System.ComponentModel.DataAnnotations;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Attendance
{
    [Key]
    public int AttendanceId { get; set; }

    public int EmpId { get; set; }

    [Required]
    public DateTime CheckIn { get; set; }

    public DateTime? CheckOut { get; set; }

    public double? TotalHours { get; set; }

    [Required]
    public WorkMode WorkMode { get; set; }

    [Required]
    public DateTime AttendanceDate { get; set; }

    public Employee Employee { get; set; } = null!;
}
