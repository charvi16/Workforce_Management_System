namespace WMS.Application.DTOs.Leaves;

public class LeaveStatisticsDto
{
    public int TotalRequests { get; set; }

    public int PendingRequests { get; set; }

    public int ApprovedRequests { get; set; }

    public int RejectedRequests { get; set; }

    public int CancelledRequests { get; set; }

    public int ApprovedDays { get; set; }
}
