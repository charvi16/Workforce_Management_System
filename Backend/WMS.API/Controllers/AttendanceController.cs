using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WMS.Application.Common;
using WMS.Application.DTOs.Attendance;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager,Employee")]
[Route("api/v1/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpGet("employees")]
    public async Task<IActionResult> Employees(CancellationToken cancellationToken)
    {
        try
        {
            var employees = await _attendanceService.GetAvailableEmployeesAsync(CurrentRole, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<AttendanceEmployeeDto>>.Ok(employees, "Attendance employees retrieved successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<IReadOnlyList<AttendanceEmployeeDto>>.Fail("Attendance employees lookup failed.", ex.Message));
        }
    }

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn(CheckInRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var attendance = await _attendanceService.CheckInAsync(request, CurrentRole, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<AttendanceDto>.Ok(attendance, "Check-in recorded successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AttendanceDto>.Fail("Check-in failed.", ex.Message));
        }
    }

    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut(CheckOutRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var attendance = await _attendanceService.CheckOutAsync(request, CurrentRole, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<AttendanceDto>.Ok(attendance, "Check-out recorded successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AttendanceDto>.Fail("Check-out failed.", ex.Message));
        }
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> Monthly(
        [FromQuery] int? employeeId,
        [FromQuery] int? departmentId,
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var records = await _attendanceService.GetMonthlyAttendanceAsync(employeeId, departmentId, month, year, CurrentRole, CurrentEmployeeId, pageNumber, pageSize, cancellationToken);
            return Ok(ApiResponse<WMS.Application.Common.PagedResult<AttendanceDto>>.Ok(records, "Monthly attendance retrieved successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<WMS.Application.Common.PagedResult<AttendanceDto>>.Fail("Monthly attendance lookup failed.", ex.Message));
        }
    }

    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    private int CurrentEmployeeId => int.TryParse(User.FindFirstValue("employee_id"), out var employeeId) ? employeeId : 0;
}
