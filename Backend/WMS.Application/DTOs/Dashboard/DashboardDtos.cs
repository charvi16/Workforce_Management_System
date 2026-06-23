namespace WMS.Application.DTOs.Dashboard;

public class DashboardKpisDto
{
    public int TotalEmployees { get; set; }
    public int TotalDepartments { get; set; }
    public int ActiveEmployees { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public int OnLeaveToday { get; set; }
    public double AttendanceRate { get; set; }
    public int PendingLeaves { get; set; }
    public int ActiveProjects { get; set; }
    public int DelayedProjects { get; set; }
    public int TotalClients { get; set; }
    public int UnallocatedEmployees { get; set; }
    public double AverageWorkingHours { get; set; }
    public int LateCheckInsToday { get; set; }
}

public class DashboardChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class DashboardAlertDto
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class DashboardTableRowDto
{
    public string Name { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

public class DashboardResponseDto
{
    public DashboardKpisDto Kpis { get; set; } = new();

    public IReadOnlyList<DashboardChartPointDto> AttendanceTrend { get; set; } = Array.Empty<DashboardChartPointDto>();

    public IReadOnlyList<DashboardChartPointDto> AttendanceDistribution { get; set; } = Array.Empty<DashboardChartPointDto>();

    public IReadOnlyList<DashboardChartPointDto> LeaveStatistics { get; set; } = Array.Empty<DashboardChartPointDto>();

    public IReadOnlyList<DashboardChartPointDto> ProjectStatusDistribution { get; set; } = Array.Empty<DashboardChartPointDto>();

    public IReadOnlyList<DashboardChartPointDto> DepartmentEmployeeCount { get; set; } = Array.Empty<DashboardChartPointDto>();

    public IReadOnlyList<DashboardChartPointDto> WorkModeDistribution { get; set; } = Array.Empty<DashboardChartPointDto>();

    public IReadOnlyList<DashboardAlertDto> Alerts { get; set; } = Array.Empty<DashboardAlertDto>();

    public IReadOnlyList<DashboardTableRowDto> TodayAttendance { get; set; } = Array.Empty<DashboardTableRowDto>();

    public IReadOnlyList<DashboardTableRowDto> ProjectRows { get; set; } = Array.Empty<DashboardTableRowDto>();

    public IReadOnlyList<string> PendingApprovals { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Announcements { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> RecentActivities { get; set; } = Array.Empty<string>();
}
