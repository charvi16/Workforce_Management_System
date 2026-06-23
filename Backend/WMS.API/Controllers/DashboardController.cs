using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WMS.Application.Common;
using WMS.Application.DTOs.Dashboard;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Admin(CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardService.GetAdminDashboardAsync(CurrentEmployeeId, cancellationToken);
        return Ok(ApiResponse<DashboardResponseDto>.Ok(dashboard, "Admin dashboard retrieved successfully."));
    }

    [HttpGet("manager")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Manager(CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardService.GetManagerDashboardAsync(CurrentEmployeeId, cancellationToken);
        return Ok(ApiResponse<DashboardResponseDto>.Ok(dashboard, "Manager dashboard retrieved successfully."));
    }

    [HttpGet("employee")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> Employee(CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardService.GetEmployeeDashboardAsync(CurrentEmployeeId, cancellationToken);
        return Ok(ApiResponse<DashboardResponseDto>.Ok(dashboard, "Employee dashboard retrieved successfully."));
    }

    private int CurrentEmployeeId => int.TryParse(User.FindFirstValue("employee_id"), out var employeeId) ? employeeId : 0;
}
