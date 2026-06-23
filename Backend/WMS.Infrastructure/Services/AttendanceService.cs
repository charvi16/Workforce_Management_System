using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using WMS.Application.Common;
using WMS.Application.DTOs.Attendance;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
    private const int CheckOutWindowHours = 12;

    private readonly WmsDbContext _dbContext;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(WmsDbContext dbContext, ILogger<AttendanceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AttendanceEmployeeDto>> GetAvailableEmployeesAsync(string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
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

    public async Task<AttendanceDto> CheckInAsync(CheckInRequestDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (currentEmployeeId <= 0)
        {
            throw new InvalidOperationException("No employee profile is linked to this login.");
        }

        if (request.EmployeeId <= 0)
        {
            request.EmployeeId = currentEmployeeId;
        }

        if (!Enum.IsDefined(typeof(WorkMode), request.WorkMode))
        {
            throw new InvalidOperationException("Invalid work mode selected.");
        }

        EnsureCanRecordOwnAttendance(request.EmployeeId, currentEmployeeId);

        var employee = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.EmployeeId == request.EmployeeId)
            .Select(e => new
            {
                e.EmployeeId,
                EmployeeName = (e.FirstName + " " + e.LastName).Trim()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Employee not found.");

        var now = DateTime.UtcNow;
        await AutoCloseExpiredOpenAttendancesAsync(now, request.EmployeeId, cancellationToken);

        var today = now.Date;
        var existingAttendance = await _dbContext.Attendances
            .AsNoTracking()
            .Where(a => a.EmpId == request.EmployeeId && (!a.CheckOut.HasValue || a.AttendanceDate == today))
            .OrderByDescending(a => !a.CheckOut.HasValue)
            .ThenByDescending(a => a.CheckIn)
            .Select(a => new AttendanceDto
            {
                AttendanceId = a.AttendanceId,
                EmployeeId = a.EmpId,
                EmployeeName = (a.Employee.FirstName + " " + a.Employee.LastName).Trim(),
                AttendanceDate = a.AttendanceDate,
                CheckIn = a.CheckIn,
                CheckOut = a.CheckOut,
                TotalHours = a.TotalHours,
                WorkMode = (int)a.WorkMode,
                WorkModeName = a.WorkMode.ToString(),
                Status = a.CheckOut.HasValue ? "Checked Out" : "Checked In"
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAttendance is not null)
        {
            return existingAttendance;
        }

        var attendance = new Attendance
        {
            EmpId = request.EmployeeId,
            CheckIn = now,
            AttendanceDate = today,
            WorkMode = (WorkMode)request.WorkMode
        };

        _dbContext.Attendances.Add(attendance);
        await _dbContext.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(
            "Attendance check-in completed in {ElapsedMs}ms for employeeId={EmployeeId}.",
            stopwatch.ElapsedMilliseconds,
            request.EmployeeId);

        return new AttendanceDto
        {
            AttendanceId = attendance.AttendanceId,
            EmployeeId = employee.EmployeeId,
            EmployeeName = employee.EmployeeName,
            AttendanceDate = attendance.AttendanceDate,
            CheckIn = attendance.CheckIn,
            CheckOut = attendance.CheckOut,
            TotalHours = attendance.TotalHours,
            WorkMode = (int)attendance.WorkMode,
            WorkModeName = attendance.WorkMode.ToString(),
            Status = "Checked In"
        };
    }

    public async Task<AttendanceDto> CheckOutAsync(CheckOutRequestDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        EnsureCanRecordOwnAttendance(request.EmployeeId, currentEmployeeId);

        var now = DateTime.UtcNow;
        var attendance = await _dbContext.Attendances
            .Include(a => a.Employee)
            .Where(a => a.EmpId == request.EmployeeId && !a.CheckOut.HasValue)
            .OrderByDescending(a => a.CheckIn)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Employee does not have an open attendance record.");

        var latestAllowedCheckout = attendance.CheckIn.AddHours(CheckOutWindowHours);
        attendance.CheckOut = now > latestAllowedCheckout ? latestAllowedCheckout : now;
        attendance.TotalHours = Math.Round((attendance.CheckOut.Value - attendance.CheckIn).TotalHours, 2);

        if (now > latestAllowedCheckout)
        {
            _dbContext.Announcements.Add(CreateAutoCheckoutAnnouncement(attendance, currentEmployeeId));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(attendance);
    }

    public async Task<PagedResult<AttendanceDto>> GetMonthlyAttendanceAsync(int? employeeId, int? departmentId, int month, int year, string currentUserRole, int currentEmployeeId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (month < 1 || month > 12)
        {
            throw new InvalidOperationException("Month must be between 1 and 12.");
        }

        if (year < 2000 || year > DateTime.UtcNow.Year + 1)
        {
            throw new InvalidOperationException("Year is outside the supported attendance range.");
        }

        var requestedEmployeeId = employeeId.GetValueOrDefault();
        if (requestedEmployeeId > 0)
        {
            await EnsureCanViewEmployeeAttendanceAsync(requestedEmployeeId, currentUserRole, currentEmployeeId, cancellationToken);

            if (!await _dbContext.Employees.AnyAsync(e => e.EmployeeId == requestedEmployeeId, cancellationToken))
            {
                throw new InvalidOperationException("Employee not found.");
            }
        }

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var stopwatch = Stopwatch.StartNew();

        await AutoCloseExpiredOpenAttendancesAsync(DateTime.UtcNow, null, cancellationToken);

        // Monthly attendance filtering remains on IQueryable so SQL receives date, role, employee, department, and paging filters before execution.
        var query = _dbContext.Attendances
            .AsNoTracking()
            .Where(a => a.AttendanceDate >= startDate && a.AttendanceDate < endDate);

        if (requestedEmployeeId > 0)
        {
            query = query.Where(a => a.EmpId == requestedEmployeeId);
        }
        else if (IsEmployeeRole(currentUserRole))
        {
            query = query.Where(a => a.EmpId == currentEmployeeId);
        }
        else if (IsManagerRole(currentUserRole))
        {
            var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken)
                ?? throw new InvalidOperationException("Current employee not found.");
            query = query.Where(a => a.EmpId == currentEmployeeId || (a.Employee.DepartmentId == currentEmployee.DepartmentId && a.Employee.Role.RoleName == nameof(UserRole.Employee)));
        }

        if (departmentId.GetValueOrDefault() > 0)
        {
            query = query.Where(a => a.Employee.DepartmentId == departmentId!.Value);
        }

        _logger.LogInformation(
            "Monthly attendance query started. Filters: employeeId={EmployeeId}, departmentId={DepartmentId}, month={Month}, year={Year}, role={Role}, pageNumber={PageNumber}, pageSize={PageSize}",
            requestedEmployeeId > 0 ? requestedEmployeeId : null,
            departmentId,
            month,
            year,
            currentUserRole,
            pageNumber,
            pageSize);

        var totalCount = await query.CountAsync(cancellationToken);
        var records = await query
            .OrderByDescending(a => a.AttendanceDate)
            .ThenByDescending(a => a.CheckIn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.AttendanceId,
                EmployeeId = a.EmpId,
                EmployeeName = (a.Employee.FirstName + " " + a.Employee.LastName).Trim(),
                a.AttendanceDate,
                a.CheckIn,
                a.CheckOut,
                a.TotalHours,
                a.WorkMode
            })
            .ToListAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(
            "Monthly attendance query completed in {ElapsedMs} ms. Returned {ReturnedCount} of {TotalCount} records.",
            stopwatch.ElapsedMilliseconds,
            records.Count,
            totalCount);

        return new PagedResult<AttendanceDto>
        {
            Items = records.Select(a => new AttendanceDto
            {
                AttendanceId = a.AttendanceId,
                EmployeeId = a.EmployeeId,
                EmployeeName = a.EmployeeName,
                AttendanceDate = a.AttendanceDate,
                CheckIn = a.CheckIn,
                CheckOut = a.CheckOut,
                TotalHours = a.TotalHours,
                WorkMode = (int)a.WorkMode,
                WorkModeName = a.WorkMode.ToString(),
                Status = a.CheckOut.HasValue ? "Checked Out" : "Checked In"
            }).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static void EnsureCanRecordOwnAttendance(int employeeId, int currentEmployeeId)
    {
        if (employeeId != currentEmployeeId)
        {
            throw new InvalidOperationException("Users can only check in or check out for themselves.");
        }
    }

    private async Task EnsureCanViewEmployeeAttendanceAsync(int employeeId, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken)
    {
        if (IsAdminRole(currentUserRole))
        {
            return;
        }

        var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken)
            ?? throw new InvalidOperationException("Current employee not found.");

        var employee = await _dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, cancellationToken)
            ?? throw new InvalidOperationException("Employee not found.");

        var allowed = employee.EmployeeId == currentEmployeeId ||
            (IsManagerRole(currentUserRole) && employee.DepartmentId == currentEmployee.DepartmentId && employee.Role.RoleName == nameof(UserRole.Employee));

        if (!allowed)
        {
            throw new InvalidOperationException("You do not have permission to view this employee's attendance.");
        }
    }

    private async Task<Employee?> GetCurrentEmployeeAsync(int currentEmployeeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.EmployeeId == currentEmployeeId, cancellationToken);
    }

    private async Task AutoCloseExpiredOpenAttendancesAsync(DateTime now, int? employeeId, CancellationToken cancellationToken)
    {
        var cutoff = now.AddHours(-CheckOutWindowHours);
        var query = _dbContext.Attendances
            .Where(a => !a.CheckOut.HasValue && a.CheckIn <= cutoff);

        if (employeeId.GetValueOrDefault() > 0)
        {
            query = query.Where(a => a.EmpId == employeeId!.Value);
        }

        var updatedCount = await query.ExecuteUpdateAsync(setters => setters
            .SetProperty(a => a.CheckOut, a => (DateTime?)a.CheckIn.AddHours(CheckOutWindowHours))
            .SetProperty(a => a.TotalHours, (double?)CheckOutWindowHours),
            cancellationToken);

        if (updatedCount > 0)
        {
            _logger.LogInformation(
                "Auto-closed {AttendanceCount} attendance record(s) at the {WindowHours}-hour checkout limit.",
                updatedCount,
                CheckOutWindowHours);
        }
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

    private static Announcement CreateAutoCheckoutAnnouncement(Attendance attendance, int currentEmployeeId)
    {
        var employeeName = $"{attendance.Employee.FirstName} {attendance.Employee.LastName}".Trim();
        return new Announcement
        {
            Title = "Attendance auto checkout assigned",
            Message = $"{employeeName} did not check out within 12 hours. Checkout was automatically assigned at 12 hours from check-in.",
            CreatedBy = currentEmployeeId,
            CreatedOn = DateTime.UtcNow,
            IsActive = true
        };
    }

    private static AttendanceEmployeeDto MapEmployeeToDto(Employee employee)
    {
        return new AttendanceEmployeeDto
        {
            EmployeeId = employee.EmployeeId,
            FullName = $"{employee.FirstName} {employee.LastName}".Trim(),
            Email = employee.Email,
            RoleName = employee.Role.RoleName
        };
    }

    private static AttendanceDto MapToDto(Attendance attendance)
    {
        return new AttendanceDto
        {
            AttendanceId = attendance.AttendanceId,
            EmployeeId = attendance.EmpId,
            EmployeeName = $"{attendance.Employee.FirstName} {attendance.Employee.LastName}",
            AttendanceDate = attendance.AttendanceDate,
            CheckIn = attendance.CheckIn,
            CheckOut = attendance.CheckOut,
            TotalHours = attendance.TotalHours,
            WorkMode = (int)attendance.WorkMode,
            WorkModeName = attendance.WorkMode.ToString(),
            Status = attendance.CheckOut.HasValue ? "Checked Out" : "Checked In"
        };
    }
}
