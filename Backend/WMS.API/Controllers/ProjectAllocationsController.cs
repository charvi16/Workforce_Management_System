using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS.Application.Common;
using WMS.Application.DTOs.Employees;
using WMS.Application.DTOs.ProjectAllocations;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager,Employee")]
[Route("api/v1/[controller]")]
public class ProjectAllocationsController : ControllerBase
{
    private readonly IProjectAllocationService _allocationService;

    public ProjectAllocationsController(IProjectAllocationService allocationService)
    {
        _allocationService = allocationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var allocations = await _allocationService.GetAllAsync(CurrentRole, CurrentEmployeeId, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProjectAllocationDto>>.Ok(allocations, "Project allocations retrieved successfully."));
    }

    [HttpGet("employees")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Employees(CancellationToken cancellationToken)
    {
        try
        {
            var employees = await _allocationService.GetAssignableEmployeesAsync(CurrentRole, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<EmployeeDto>>.Ok(employees, "Assignable project employees retrieved successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<IReadOnlyList<EmployeeDto>>.Fail("Assignable project employees lookup failed.", ex.Message));
        }
    }

    [HttpGet("project/{projectId:int}")]
    public async Task<IActionResult> GetByProject(int projectId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var allocations = await _allocationService.GetByProjectAsync(projectId, CurrentRole, CurrentEmployeeId, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProjectAllocationDto>>.Ok(allocations, "Project allocations retrieved successfully."));
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var allocations = await _allocationService.GetByEmployeeAsync(employeeId, CurrentRole, CurrentEmployeeId, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProjectAllocationDto>>.Ok(allocations, "Employee allocations retrieved successfully."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(ProjectAllocationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var allocation = await _allocationService.CreateAsync(request, CurrentRole, CurrentEmployeeId, cancellationToken);
            return CreatedAtAction(nameof(GetByProject), new { projectId = allocation.ProjectId }, ApiResponse<ProjectAllocationDto>.Ok(allocation, "Project allocation created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ProjectAllocationDto>.Fail("Project allocation creation failed.", ex.Message));
        }
        catch (DbUpdateException ex)
        {
            return BadRequest(ApiResponse<ProjectAllocationDto>.Fail("Project allocation creation failed.", ex.InnerException?.Message ?? ex.Message));
        }
    }

    [HttpPut("{allocationId:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int allocationId, ProjectAllocationRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var allocation = await _allocationService.UpdateAsync(allocationId, request, CurrentRole, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<ProjectAllocationDto>.Ok(allocation, "Project allocation updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ProjectAllocationDto>.Fail("Project allocation update failed.", ex.Message));
        }
        catch (DbUpdateException ex)
        {
            return BadRequest(ApiResponse<ProjectAllocationDto>.Fail("Project allocation update failed.", ex.InnerException?.Message ?? ex.Message));
        }
    }

    [HttpDelete("{allocationId:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int allocationId, CancellationToken cancellationToken)
    {
        try
        {
            var allocation = await _allocationService.DeleteAsync(allocationId, CurrentRole, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<ProjectAllocationDto>.Ok(allocation, "Project allocation deactivated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ProjectAllocationDto>.Fail("Project allocation deactivation failed.", ex.Message));
        }
    }

    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    private int CurrentEmployeeId => int.TryParse(User.FindFirstValue("employee_id"), out var employeeId) ? employeeId : 0;
}
