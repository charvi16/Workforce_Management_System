namespace WMS.Application.DTOs.Reports;

public class AttendanceReportRequestDto
{
    public int? EmployeeId { get; set; }

    public int? DepartmentId { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public string? ReportType { get; set; }
}
