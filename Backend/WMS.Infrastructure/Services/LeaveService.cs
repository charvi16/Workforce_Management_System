using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using WMS.Application.Common;
using WMS.Application.DTOs.Leaves;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class LeaveService : ILeaveService
{
    private readonly WmsDbContext _dbContext;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(WmsDbContext dbContext, ILogger<LeaveService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LeaveEmployeeDto>> GetAvailableEmployeesAsync(string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken);
        var query = _dbContext.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Role)
            .AsQueryable();

        if (IsEmployeeRole(currentUserRole))
        {
            query = query.Where(e => e.EmployeeId == currentEmployeeId);
        }
        else if (IsManagerRole(currentUserRole))
        {
            if (currentEmployee is null)
            {
                throw new InvalidOperationException("Current employee not found.");
            }

            query = query.Where(e => e.EmployeeId == currentEmployeeId || (e.DepartmentId == currentEmployee.DepartmentId && e.Role.RoleName == nameof(UserRole.Employee)));
        }

        var employees = await query
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync(cancellationToken);

        if (currentEmployee is not null && employees.All(e => e.EmployeeId != currentEmployee.EmployeeId))
        {
            employees.Insert(0, currentEmployee);
        }

        return employees.Select(MapEmployeeToDto).ToList();
    }

    public async Task<LeaveDto> ApplyAsync(ApplyLeaveRequestDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(LeaveType), request.LeaveType))
        {
            throw new InvalidOperationException("Invalid leave type selected.");
        }

        var fromDate = request.FromDate.Date;
        var toDate = request.ToDate.Date;

        if (fromDate > toDate)
        {
            throw new InvalidOperationException("Leave from date cannot be after the to date.");
        }

        var currentEmployee = await EnsureCurrentEmployeeAsync(currentEmployeeId, cancellationToken);
        if (request.EmployeeId != currentEmployee.EmployeeId)
        {
            throw new InvalidOperationException("Users can only apply leave for themselves.");
        }

        var employee = await _dbContext.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId, cancellationToken)
            ?? throw new InvalidOperationException("Employee not found.");

        var hasOverlap = await _dbContext.Leaves.AnyAsync(l =>
            l.EmpId == request.EmployeeId &&
            (l.Status == LeaveStatus.Pending || l.Status == (LeaveStatus)0 || l.Status == LeaveStatus.Approved) &&
            l.FromDate <= toDate &&
            l.ToDate >= fromDate,
            cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException("A pending or approved leave already exists for the selected date range.");
        }

        var now = DateTime.UtcNow;
        var isAdminLeave = IsAdminRole(employee.Role.RoleName);
        var leave = new Leave
        {
            EmpId = request.EmployeeId,
            LeaveType = (LeaveType)request.LeaveType,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            FromDate = fromDate,
            ToDate = toDate,
            Status = isAdminLeave ? LeaveStatus.Approved : LeaveStatus.Pending,
            AppliedOn = now,
            ApprovedBy = isAdminLeave ? currentEmployee.EmployeeId : null,
            ApprovedOn = isAdminLeave ? now : null,
            Approver = isAdminLeave ? currentEmployee : null
        };

        _dbContext.Leaves.Add(leave);
        await _dbContext.SaveChangesAsync(cancellationToken);

        leave.Employee = employee;
        return MapToDto(leave);
    }

    public async Task<LeaveDto> CancelAsync(int leaveId, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        var leave = await LoadLeaveAsync(leaveId, cancellationToken);
        EnsureCanAccessOwnLeave(leave.EmpId, currentEmployeeId);

        if (leave.Status == LeaveStatus.Cancelled)
        {
            throw new InvalidOperationException("Leave request is already cancelled.");
        }

        if (leave.Status == LeaveStatus.Rejected)
        {
            throw new InvalidOperationException("Rejected leave requests cannot be cancelled.");
        }

        leave.Status = LeaveStatus.Cancelled;
        leave.ApprovedBy = null;
        leave.ApprovedOn = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(leave);
    }

    public async Task<LeaveDto> ReviewAsync(int leaveId, ReviewLeaveRequestDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        if (IsEmployeeRole(currentUserRole))
        {
            throw new InvalidOperationException("Only admins and managers can approve or reject leave requests.");
        }

        var reviewer = await _dbContext.Employees
            .FirstOrDefaultAsync(e => e.EmployeeId == currentEmployeeId, cancellationToken);

        var leave = await LoadLeaveAsync(leaveId, cancellationToken);
        if (!IsPendingStatus(leave.Status))
        {
            throw new InvalidOperationException("Only pending leave requests can be approved or rejected.");
        }

        if (reviewer?.EmployeeId == leave.EmpId)
        {
            throw new InvalidOperationException("Users cannot approve or reject their own leave requests.");
        }

        EnsureCanReviewLeave(leave, currentUserRole);

        leave.Status = request.IsApproved ? LeaveStatus.Approved : LeaveStatus.Rejected;
        leave.ApprovedBy = reviewer?.EmployeeId;
        leave.ApprovedOn = DateTime.UtcNow;
        leave.Approver = reviewer;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(leave);
    }

    public async Task<PagedResult<LeaveDto>> GetLeavesAsync(int? employeeId, int? status, DateTime? fromDate, DateTime? toDate, string currentUserRole, int currentEmployeeId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var requestedEmployeeId = employeeId.GetValueOrDefault();
        if (requestedEmployeeId > 0)
        {
            await EnsureCanAccessEmployeeAsync(requestedEmployeeId, currentUserRole, currentEmployeeId, cancellationToken);
        }

        if (status.HasValue && !Enum.IsDefined(typeof(LeaveStatus), status.Value))
        {
            throw new InvalidOperationException("Invalid leave status selected.");
        }

        var startDate = fromDate?.Date;
        var endDate = toDate?.Date;
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            throw new InvalidOperationException("From date cannot be after to date.");
        }

        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var stopwatch = Stopwatch.StartNew();

        // Leave status filtering remains on IQueryable so SQL applies role, employee, status, date range, ordering, and paging before execution.
        var query = _dbContext.Leaves
            .AsNoTracking();

        if (IsEmployeeRole(currentUserRole))
        {
            query = query.Where(l => l.EmpId == currentEmployeeId);
        }
        else if (IsManagerRole(currentUserRole))
        {
            var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken)
                ?? throw new InvalidOperationException("Current employee not found.");
            query = query.Where(l => l.EmpId == currentEmployeeId || (l.Employee.DepartmentId == currentEmployee.DepartmentId && l.Employee.Role.RoleName == nameof(UserRole.Employee)));
        }

        if (requestedEmployeeId > 0)
        {
            query = query.Where(l => l.EmpId == requestedEmployeeId);
        }

        if (status.HasValue)
        {
            query = (LeaveStatus)status.Value == LeaveStatus.Pending
                ? query.Where(l => l.Status == LeaveStatus.Pending || l.Status == (LeaveStatus)0)
                : query.Where(l => l.Status == (LeaveStatus)status.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(l => l.ToDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.FromDate <= endDate.Value);
        }

        _logger.LogInformation(
            "Leave status query started. Filters: employeeId={EmployeeId}, status={Status}, fromDate={FromDate}, toDate={ToDate}, role={Role}, pageNumber={PageNumber}, pageSize={PageSize}",
            requestedEmployeeId > 0 ? requestedEmployeeId : null,
            status,
            startDate,
            endDate,
            currentUserRole,
            pageNumber,
            pageSize);

        var totalCount = await query.CountAsync(cancellationToken);
        var leaves = await query
            .OrderByDescending(l => l.AppliedOn)
            .ThenByDescending(l => l.FromDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.LeaveId,
                EmployeeId = l.EmpId,
                EmployeeName = (l.Employee.FirstName + " " + l.Employee.LastName).Trim(),
                l.LeaveType,
                l.Reason,
                l.FromDate,
                l.ToDate,
                l.Status,
                l.AppliedOn,
                l.ApprovedBy,
                ApproverName = l.Approver == null ? null : (l.Approver.FirstName + " " + l.Approver.LastName).Trim(),
                l.ApprovedOn
            })
            .ToListAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(
            "Leave status query completed in {ElapsedMs} ms. Returned {ReturnedCount} of {TotalCount} records.",
            stopwatch.ElapsedMilliseconds,
            leaves.Count,
            totalCount);

        return new PagedResult<LeaveDto>
        {
            Items = leaves.Select(l =>
            {
                var normalizedStatus = NormalizeStatus(l.Status);
                return new LeaveDto
                {
                    LeaveId = l.LeaveId,
                    EmployeeId = l.EmployeeId,
                    EmployeeName = l.EmployeeName,
                    LeaveType = (int)l.LeaveType,
                    LeaveTypeName = l.LeaveType.ToString(),
                    Reason = l.Reason,
                    FromDate = l.FromDate,
                    ToDate = l.ToDate,
                    TotalDays = GetTotalDays(l.FromDate, l.ToDate),
                    Status = (int)normalizedStatus,
                    StatusName = normalizedStatus.ToString(),
                    AppliedOn = l.AppliedOn,
                    ApprovedBy = l.ApprovedBy,
                    ApproverName = l.ApproverName,
                    ApprovedOn = l.ApprovedOn
                };
            }).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<LeaveStatisticsDto> GetStatisticsAsync(int? employeeId, int? year, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        var requestedEmployeeId = employeeId.GetValueOrDefault();
        if (requestedEmployeeId > 0)
        {
            await EnsureCanAccessEmployeeAsync(requestedEmployeeId, currentUserRole, currentEmployeeId, cancellationToken);
        }

        var query = _dbContext.Leaves
            .AsNoTracking();

        if (IsEmployeeRole(currentUserRole))
        {
            query = query.Where(l => l.EmpId == currentEmployeeId);
        }
        else if (IsManagerRole(currentUserRole))
        {
            var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken)
                ?? throw new InvalidOperationException("Current employee not found.");
            query = query.Where(l => l.EmpId == currentEmployeeId || (l.Employee.DepartmentId == currentEmployee.DepartmentId && l.Employee.Role.RoleName == nameof(UserRole.Employee)));
        }

        if (requestedEmployeeId > 0)
        {
            query = query.Where(l => l.EmpId == requestedEmployeeId);
        }

        if (year.HasValue)
        {
            query = query.Where(l => l.FromDate.Year == year.Value);
        }

        var statistics = await query
            .GroupBy(_ => 1)
            .Select(group => new LeaveStatisticsDto
            {
                TotalRequests = group.Count(),
                PendingRequests = group.Count(l => l.Status == LeaveStatus.Pending || l.Status == (LeaveStatus)0),
                ApprovedRequests = group.Count(l => l.Status == LeaveStatus.Approved),
                RejectedRequests = group.Count(l => l.Status == LeaveStatus.Rejected),
                CancelledRequests = group.Count(l => l.Status == LeaveStatus.Cancelled),
                ApprovedDays = group
                    .Where(l => l.Status == LeaveStatus.Approved)
                    .Sum(l => (int?)EF.Functions.DateDiffDay(l.FromDate, l.ToDate) + 1) ?? 0
            })
            .FirstOrDefaultAsync(cancellationToken);

        return statistics ?? new LeaveStatisticsDto();
    }

    private async Task<Leave> LoadLeaveAsync(int leaveId, CancellationToken cancellationToken)
    {
        return await _dbContext.Leaves
            .Include(l => l.Employee)
                .ThenInclude(e => e.Role)
            .Include(l => l.Approver)
            .FirstOrDefaultAsync(l => l.LeaveId == leaveId, cancellationToken)
            ?? throw new InvalidOperationException("Leave request not found.");
    }

    private async Task EnsureCanAccessEmployeeAsync(int employeeId, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken)
    {
        if (IsAdminRole(currentUserRole))
        {
            return;
        }

        var employee = await _dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, cancellationToken)
            ?? throw new InvalidOperationException("Employee not found.");

        var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken)
            ?? throw new InvalidOperationException("Current employee not found.");

        var allowed = employee.EmployeeId == currentEmployeeId ||
            (IsManagerRole(currentUserRole) && employee.DepartmentId == currentEmployee.DepartmentId && employee.Role.RoleName == nameof(UserRole.Employee));

        if (!allowed)
        {
            throw new InvalidOperationException("You do not have permission to access this employee's leave requests.");
        }
    }

    private static void EnsureCanAccessOwnLeave(int employeeId, int currentEmployeeId)
    {
        if (employeeId != currentEmployeeId)
        {
            throw new InvalidOperationException("Users can only cancel their own leave requests.");
        }
    }

    private async Task<Employee?> GetCurrentEmployeeAsync(int currentEmployeeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.EmployeeId == currentEmployeeId, cancellationToken);
    }

    private async Task<Employee> EnsureCurrentEmployeeAsync(int currentEmployeeId, CancellationToken cancellationToken)
    {
        return await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken)
            ?? throw new InvalidOperationException("No employee profile is linked to this login.");
    }

    private static void EnsureCanReviewLeave(Leave leave, string currentUserRole)
    {
        var applicantRole = leave.Employee.Role.RoleName;

        if (IsManagerRole(currentUserRole) && applicantRole == nameof(UserRole.Employee))
        {
            return;
        }

        if (IsAdminRole(currentUserRole) && (applicantRole == nameof(UserRole.Employee) || applicantRole == nameof(UserRole.Manager)))
        {
            return;
        }

        if (applicantRole == nameof(UserRole.Admin))
        {
            throw new InvalidOperationException("Admin leave requests are auto-approved when applied.");
        }

        throw new InvalidOperationException("You do not have permission to review this leave request.");
    }

    private static bool IsEmployeeRole(string currentUserRole)
    {
        return string.Equals(currentUserRole, nameof(UserRole.Employee), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsManagerRole(string currentUserRole)
    {
        return string.Equals(currentUserRole, nameof(UserRole.Manager), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAdminRole(string currentUserRole)
    {
        return string.Equals(currentUserRole, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase);
    }

    private static int GetTotalDays(Leave leave)
    {
        return GetTotalDays(leave.FromDate, leave.ToDate);
    }

    private static int GetTotalDays(DateTime fromDate, DateTime toDate) =>
        (toDate.Date - fromDate.Date).Days + 1;

    private static bool IsPendingStatus(LeaveStatus status) =>
        status == LeaveStatus.Pending || status == (LeaveStatus)0;

    private static LeaveStatus NormalizeStatus(LeaveStatus status) =>
        IsPendingStatus(status) ? LeaveStatus.Pending : status;

    private static LeaveEmployeeDto MapEmployeeToDto(Employee employee)
    {
        return new LeaveEmployeeDto
        {
            EmployeeId = employee.EmployeeId,
            FullName = $"{employee.FirstName} {employee.LastName}".Trim(),
            Email = employee.Email,
            RoleName = employee.Role.RoleName
        };
    }

    private static LeaveDto MapToDto(Leave leave)
    {
        return new LeaveDto
        {
            LeaveId = leave.LeaveId,
            EmployeeId = leave.EmpId,
            EmployeeName = $"{leave.Employee.FirstName} {leave.Employee.LastName}".Trim(),
            LeaveType = (int)leave.LeaveType,
            LeaveTypeName = leave.LeaveType.ToString(),
            Reason = leave.Reason,
            FromDate = leave.FromDate,
            ToDate = leave.ToDate,
            TotalDays = GetTotalDays(leave),
            Status = (int)NormalizeStatus(leave.Status),
            StatusName = NormalizeStatus(leave.Status).ToString(),
            AppliedOn = leave.AppliedOn,
            ApprovedBy = leave.ApprovedBy,
            ApproverName = leave.Approver is null ? null : $"{leave.Approver.FirstName} {leave.Approver.LastName}".Trim(),
            ApprovedOn = leave.ApprovedOn
        };
    }
}
