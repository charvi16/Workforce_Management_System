namespace WMS.Application.DTOs.Leaves;

public class LeaveDto
{
    public int LeaveId { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public int LeaveType { get; set; }

    public string LeaveTypeName { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public int TotalDays { get; set; }

    public int Status { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public DateTime AppliedOn { get; set; }

    public int? ApprovedBy { get; set; }

    public string? ApproverName { get; set; }

    public DateTime? ApprovedOn { get; set; }
}
