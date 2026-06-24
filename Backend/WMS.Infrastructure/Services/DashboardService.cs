using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Dashboard;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly WmsDbContext _dbContext;

    public DashboardService(WmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DashboardResponseDto> GetAdminDashboardAsync(int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        return BuildDashboardAsync("Admin", currentEmployeeId, cancellationToken);
    }

    public Task<DashboardResponseDto> GetManagerDashboardAsync(int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        return BuildDashboardAsync("Manager", currentEmployeeId, cancellationToken);
    }

    public Task<DashboardResponseDto> GetEmployeeDashboardAsync(int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        return BuildDashboardAsync("Employee", currentEmployeeId, cancellationToken);
    }

    private async Task<DashboardResponseDto> BuildDashboardAsync(string role, int currentEmployeeId, CancellationToken cancellationToken)
    {
        var currentEmployee = await _dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == currentEmployeeId, cancellationToken);

        var employeeScope = BuildEmployeeScope(role, currentEmployee);
        var projectScope = BuildProjectScope(role, currentEmployee);
        var employeeIds = employeeScope.Select(e => e.EmployeeId);
        var today = DateTime.UtcNow.Date;
        var lateCheckInCutoff = today.AddHours(9.5);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var totalEmployees = await employeeScope.CountAsync(cancellationToken);
        var totalDepartments = await GetDepartmentCountAsync(role, currentEmployee, cancellationToken);
        var activeEmployees = await employeeScope.CountAsync(e => e.Status == EmployeeStatus.Active, cancellationToken);
        var presentToday = await _dbContext.Attendances.AsNoTracking()
            .Where(a => a.AttendanceDate == today && employeeIds.Contains(a.EmpId))
            .CountAsync(cancellationToken);
        var onLeaveToday = await _dbContext.Leaves.AsNoTracking()
            .Where(l => l.Status == LeaveStatus.Approved && l.FromDate <= today && l.ToDate >= today && employeeIds.Contains(l.EmpId))
            .CountAsync(cancellationToken);
        var pendingLeaves = await _dbContext.Leaves.AsNoTracking()
            .Where(l => l.Status == LeaveStatus.Pending && employeeIds.Contains(l.EmpId))
            .CountAsync(cancellationToken);
        var lateCheckInsToday = await _dbContext.Attendances.AsNoTracking()
            .Where(a => a.AttendanceDate == today && a.CheckIn > lateCheckInCutoff && employeeIds.Contains(a.EmpId))
            .CountAsync(cancellationToken);
        var delayedProjects = await projectScope.CountAsync(p => p.Status == "Delayed" || (p.Status != "Completed" && p.Status != "Cancelled" && p.EndDate.HasValue && p.EndDate.Value.Date < today), cancellationToken);
        var activeProjects = await projectScope.CountAsync(p => p.Status == "Active" || (p.Status != "Completed" && p.Status != "Cancelled" && (!p.StartDate.HasValue || p.StartDate.Value.Date <= today) && (!p.EndDate.HasValue || p.EndDate.Value.Date >= today)), cancellationToken);
        var totalClients = string.Equals(role, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase)
            ? await _dbContext.Clients.AsNoTracking().CountAsync(c => c.Status, cancellationToken)
            : await projectScope
                .Where(p => p.ClientId.HasValue && p.Client != null && p.Client.Status)
                .Select(p => p.ClientId!.Value)
                .Distinct()
                .CountAsync(cancellationToken);
        var allocatedEmployeeIds = _dbContext.EmployeeProjectAllocations.AsNoTracking()
            .Where(a => a.Status)
            .Select(a => a.EmpId)
            .Distinct();
        var unallocatedEmployees = await employeeScope.CountAsync(e => !allocatedEmployeeIds.Contains(e.EmployeeId), cancellationToken);
        var averageWorkingHours = await _dbContext.Attendances.AsNoTracking()
            .Where(a => a.AttendanceDate >= monthStart && a.AttendanceDate < monthEnd && employeeIds.Contains(a.EmpId) && a.TotalHours.HasValue)
            .Select(a => a.TotalHours)
            .DefaultIfEmpty()
            .AverageAsync(cancellationToken) ?? 0d;

        var attendanceTrendRows = await _dbContext.Attendances.AsNoTracking()
            .Where(a => a.AttendanceDate >= monthStart && a.AttendanceDate < monthEnd && employeeIds.Contains(a.EmpId))
            .GroupBy(a => a.AttendanceDate.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(g => g.Date)
            .ToListAsync(cancellationToken);

        var attendanceTrend = attendanceTrendRows
            .Select(g => new DashboardChartPointDto { Label = g.Date.ToString("dd MMM"), Value = g.Count })
            .ToList();

        var attendanceDistribution = new List<DashboardChartPointDto>
        {
            new() { Label = "Present", Value = presentToday },
            new() { Label = "Absent", Value = Math.Max(activeEmployees - presentToday - onLeaveToday, 0) },
            new() { Label = "Leave", Value = onLeaveToday }
        };

        var leaveStatistics = await _dbContext.Leaves.AsNoTracking()
            .Where(l => employeeIds.Contains(l.EmpId))
            .GroupBy(l => l.Status)
            .Select(g => new DashboardChartPointDto { Label = g.Key.ToString(), Value = g.Count() })
            .ToListAsync(cancellationToken);

        var projectStatusDistribution = await projectScope
            .GroupBy(p => p.Status)
            .Select(g => new DashboardChartPointDto { Label = g.Key, Value = g.Count() })
            .ToListAsync(cancellationToken);

        var departmentEmployeeCount = await employeeScope
            .GroupBy(e => e.Department == null ? "Unassigned" : e.Department.DepartmentName)
            .Select(g => new DashboardChartPointDto { Label = g.Key, Value = g.Count() })
            .ToListAsync(cancellationToken);

        var workModeDistribution = await _dbContext.Attendances.AsNoTracking()
            .Where(a => a.AttendanceDate >= monthStart && a.AttendanceDate < monthEnd && employeeIds.Contains(a.EmpId))
            .GroupBy(a => a.WorkMode)
            .Select(g => new DashboardChartPointDto { Label = g.Key.ToString(), Value = g.Count() })
            .ToListAsync(cancellationToken);

        var alerts = BuildAlerts(role, pendingLeaves, delayedProjects, unallocatedEmployees, lateCheckInsToday);
        var todayAttendanceRows = await _dbContext.Attendances.AsNoTracking()
            .Where(a => a.AttendanceDate == today && employeeIds.Contains(a.EmpId))
            .OrderByDescending(a => a.CheckIn)
            .Take(5)
            .Select(a => new
            {
                EmployeeName = (a.Employee.FirstName + " " + a.Employee.LastName).Trim(),
                a.CheckIn,
                a.CheckOut
            })
            .ToListAsync(cancellationToken);

        var todayAttendance = todayAttendanceRows
            .Select(a => new DashboardTableRowDto
            {
                Name = role == nameof(UserRole.Employee) ? "Today" : a.EmployeeName,
                Detail = a.CheckIn.ToLocalTime().ToString("hh:mm tt"),
                Status = a.CheckOut.HasValue ? "Checked Out" : "Checked In"
            })
            .ToList();

        var projectRows = await projectScope
            .OrderBy(p => p.Status == "Active" ? 0 : 1)
            .ThenBy(p => p.EndDate ?? DateTime.MaxValue)
            .Take(5)
            .Select(p => new DashboardTableRowDto
            {
                Name = p.ProjectName,
                Detail = p.Client == null ? "--" : p.Client.ClientName,
                Status = p.Status
            })
            .ToListAsync(cancellationToken);

        var pendingApprovalRows = await _dbContext.Leaves.AsNoTracking()
            .Where(l => l.Status == LeaveStatus.Pending && employeeIds.Contains(l.EmpId))
            .OrderByDescending(l => l.AppliedOn)
            .Take(5)
            .ToListAsync(cancellationToken);

        var pendingApprovals = pendingApprovalRows
            .Select(l => $"Leave #{l.LeaveId} pending")
            .ToList();

        var announcements = await _dbContext.Announcements.AsNoTracking()
            .Where(a =>
                a.IsActive
                && (!a.ExpiryDate.HasValue || a.ExpiryDate.Value.Date >= today)
                && (a.TargetRole == null || a.TargetRole == string.Empty || a.TargetRole.ToLower() == role.ToLower()))
            .OrderByDescending(a => a.CreatedOn)
            .Take(5)
            .Select(a => $"{a.Title}: {a.Message}")
            .ToListAsync(cancellationToken);

        var recentActivityRows = await _dbContext.Attendances.AsNoTracking()
            .Where(a => employeeIds.Contains(a.EmpId))
            .OrderByDescending(a => a.AttendanceDate)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentActivities = recentActivityRows
            .Select(a => $"Attendance on {a.AttendanceDate:dd MMM}")
            .ToList();

        return new DashboardResponseDto
        {
            Kpis = new DashboardKpisDto
            {
                TotalEmployees = totalEmployees,
                TotalDepartments = totalDepartments,
                ActiveEmployees = activeEmployees,
                PresentToday = presentToday,
                AbsentToday = Math.Max(activeEmployees - presentToday - onLeaveToday, 0),
                OnLeaveToday = onLeaveToday,
                AttendanceRate = activeEmployees == 0 ? 0 : Math.Round((presentToday * 100.0) / activeEmployees, 1),
                PendingLeaves = pendingLeaves,
                ActiveProjects = activeProjects,
                DelayedProjects = delayedProjects,
                TotalClients = totalClients,
                UnallocatedEmployees = unallocatedEmployees,
                AverageWorkingHours = Math.Round(averageWorkingHours, 1),
                LateCheckInsToday = lateCheckInsToday
            },
            AttendanceTrend = attendanceTrend,
            AttendanceDistribution = attendanceDistribution,
            LeaveStatistics = leaveStatistics,
            ProjectStatusDistribution = projectStatusDistribution,
            DepartmentEmployeeCount = departmentEmployeeCount,
            WorkModeDistribution = workModeDistribution,
            Alerts = alerts,
            TodayAttendance = todayAttendance,
            ProjectRows = projectRows,
            PendingApprovals = pendingApprovals,
            Announcements = announcements,
            RecentActivities = recentActivities
        };
    }

    private IQueryable<Employee> BuildEmployeeScope(string role, Employee? currentEmployee)
    {
        if (string.Equals(role, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase))
        {
            return _dbContext.Employees.AsNoTracking();
        }

        if (currentEmployee is null)
        {
            return _dbContext.Employees.AsNoTracking().Where(e => false);
        }

        if (string.Equals(role, nameof(UserRole.Employee), StringComparison.OrdinalIgnoreCase))
        {
            return _dbContext.Employees.AsNoTracking().Where(e => e.EmployeeId == currentEmployee.EmployeeId);
        }

        return _dbContext.Employees.AsNoTracking().Where(e => e.EmployeeId == currentEmployee.EmployeeId || (e.DepartmentId == currentEmployee.DepartmentId && e.Role.RoleName == nameof(UserRole.Employee)));
    }

    private IQueryable<Project> BuildProjectScope(string role, Employee? currentEmployee)
    {
        if (string.Equals(role, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase))
        {
            return _dbContext.Projects.AsNoTracking();
        }

        if (currentEmployee is null)
        {
            return _dbContext.Projects.AsNoTracking().Where(p => false);
        }

        if (string.Equals(role, nameof(UserRole.Employee), StringComparison.OrdinalIgnoreCase))
        {
            return _dbContext.Projects.AsNoTracking()
                .Where(p => p.EmployeeAllocations.Any(a => a.Status && a.EmpId == currentEmployee.EmployeeId));
        }

        return _dbContext.Projects.AsNoTracking()
            .Where(p => p.EmployeeAllocations.Any(a => a.Status && (a.EmpId == currentEmployee.EmployeeId || (a.Employee.DepartmentId == currentEmployee.DepartmentId && a.Employee.Role.RoleName == nameof(UserRole.Employee)))));
    }

    private async Task<int> GetDepartmentCountAsync(string role, Employee? currentEmployee, CancellationToken cancellationToken)
    {
        if (string.Equals(role, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase))
        {
            return await _dbContext.Departments.AsNoTracking().CountAsync(cancellationToken);
        }

        return currentEmployee?.DepartmentId is null ? 0 : 1;
    }

    private static IReadOnlyList<DashboardAlertDto> BuildAlerts(string role, int pendingLeaves, int delayedProjects, int unallocatedEmployees, int lateCheckInsToday)
    {
        var alerts = new List<DashboardAlertDto>();

        if (string.Equals(role, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase))
        {
            alerts.Add(new DashboardAlertDto { Type = lateCheckInsToday > 0 ? "Warning" : "Info", Message = $"{lateCheckInsToday} employees checked in late today." });
            alerts.Add(new DashboardAlertDto { Type = delayedProjects > 0 ? "Critical" : "Success", Message = $"{delayedProjects} projects are delayed." });
            alerts.Add(new DashboardAlertDto { Type = pendingLeaves > 0 ? "Warning" : "Info", Message = $"{pendingLeaves} leave requests are pending." });
            alerts.Add(new DashboardAlertDto { Type = unallocatedEmployees > 0 ? "Warning" : "Success", Message = $"{unallocatedEmployees} employees are not allocated to any active project." });
            return alerts;
        }

        if (string.Equals(role, nameof(UserRole.Manager), StringComparison.OrdinalIgnoreCase))
        {
            alerts.Add(new DashboardAlertDto { Type = pendingLeaves > 0 ? "Warning" : "Info", Message = $"{pendingLeaves} team leave requests need action." });
            alerts.Add(new DashboardAlertDto { Type = delayedProjects > 0 ? "Critical" : "Success", Message = $"{delayedProjects} team projects are delayed." });
            alerts.Add(new DashboardAlertDto { Type = lateCheckInsToday > 0 ? "Warning" : "Info", Message = $"{lateCheckInsToday} team members checked in late today." });
            return alerts;
        }

        alerts.Add(new DashboardAlertDto { Type = "Warning", Message = "Check in status and leave requests are available in your dashboard." });
        return alerts;
    }
}
