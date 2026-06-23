using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WMS.Application.Common;
using WMS.Application.DTOs.Leaves;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager,Employee")]
[Route("api/v1/[controller]")]
public class LeavesController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeavesController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpGet("employees")]
    public async Task<IActionResult> Employees(CancellationToken cancellationToken)
    {
        try
        {
            var employees = await _leaveService.GetAvailableEmployeesAsync(CurrentRole, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<LeaveEmployeeDto>>.Ok(employees, "Leave employees retrieved successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<IReadOnlyList<LeaveEmployeeDto>>.Fail("Leave employees lookup failed.", ex.Message));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetLeaves(
        [FromQuery] int? employeeId,
        [FromQuery] int? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var leaves = await _leaveService.GetLeavesAsync(employeeId, status, fromDate, toDate, CurrentRole, CurrentEmployeeId, pageNumber, pageSize, cancellationToken);
            return Ok(ApiResponse<WMS.Application.Common.PagedResult<LeaveDto>>.Ok(leaves, "Leave requests retrieved successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<WMS.Application.Common.PagedResult<LeaveDto>>.Fail("Leave request lookup failed.", ex.Message));
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(
        [FromQuery] int? employeeId,
        [FromQuery] int? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var leaves = await _leaveService.GetLeavesAsync(employeeId, status, fromDate, toDate, CurrentRole, CurrentEmployeeId, pageNumber, pageSize, cancellationToken);
            return Ok(ApiResponse<WMS.Application.Common.PagedResult<LeaveDto>>.Ok(leaves, "Leave requests retrieved successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<WMS.Application.Common.PagedResult<LeaveDto>>.Fail("Leave request lookup failed.", ex.Message));
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> Statistics([FromQuery] int? employeeId, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        try
        {
            var statistics = await _leaveService.GetStatisticsAsync(employeeId, year, CurrentRole, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<LeaveStatisticsDto>.Ok(statistics, "Leave statistics retrieved successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LeaveStatisticsDto>.Fail("Leave statistics lookup failed.", ex.Message));
        }
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply(ApplyLeaveRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var leave = await _leaveService.ApplyAsync(request, CurrentRole, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<LeaveDto>.Ok(leave, "Leave request submitted successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LeaveDto>.Fail("Leave request failed.", ex.Message));
        }
    }

    [HttpPut("{leaveId:int}/cancel")]
    public async Task<IActionResult> Cancel(int leaveId, CancellationToken cancellationToken)
    {
        try
        {
            var leave = await _leaveService.CancelAsync(leaveId, CurrentRole, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<LeaveDto>.Ok(leave, "Leave request cancelled successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LeaveDto>.Fail("Leave cancellation failed.", ex.Message));
        }
    }

    [HttpPut("{leaveId:int}/review")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Review(int leaveId, ReviewLeaveRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var leave = await _leaveService.ReviewAsync(leaveId, request, CurrentRole, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<LeaveDto>.Ok(leave, request.IsApproved ? "Leave request approved successfully." : "Leave request rejected successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LeaveDto>.Fail("Leave review failed.", ex.Message));
        }
    }

    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    private int CurrentEmployeeId => int.TryParse(User.FindFirstValue("employee_id"), out var employeeId) ? employeeId : 0;
}
