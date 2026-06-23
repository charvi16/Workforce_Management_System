namespace WMS.Application.DTOs.Attendance;

public class AttendanceDto
{
    public int AttendanceId { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public DateTime AttendanceDate { get; set; }

    public DateTime CheckIn { get; set; }

    public DateTime? CheckOut { get; set; }

    public double? TotalHours { get; set; }

    public int WorkMode { get; set; }

    public string WorkModeName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
