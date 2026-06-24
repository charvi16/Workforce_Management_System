using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS.Application.Common;
using WMS.Application.DTOs.Projects;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Employee")]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int? clientId, [FromQuery] string? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var projects = await _projectService.GetAllAsync(CurrentRole, CurrentEmployeeId, search, clientId, status, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProjectDto>>.Ok(projects, "Projects retrieved successfully."));
    }

    [HttpGet("{projectId:int}")]
    [Authorize(Roles = "Admin,Manager,Employee")]
    public async Task<IActionResult> GetById(int projectId, CancellationToken cancellationToken)
    {
        try
        {
            var project = await _projectService.GetByIdAsync(projectId, CurrentRole, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<ProjectDto>.Ok(project, "Project retrieved successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<ProjectDto>.Fail("Project lookup failed.", ex.Message));
        }
    }

    [HttpGet("by-client/{clientId:int}")]
    [Authorize(Roles = "Admin,Manager,Employee")]
    public async Task<IActionResult> GetByClient(int clientId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var projects = await _projectService.GetByClientAsync(clientId, CurrentRole, CurrentEmployeeId, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProjectDto>>.Ok(projects, "Client projects retrieved successfully."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(ProjectRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var project = await _projectService.CreateAsync(request, CurrentEmployeeId, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { projectId = project.ProjectId }, ApiResponse<ProjectDto>.Ok(project, "Project created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ProjectDto>.Fail("Project creation failed.", ex.Message));
        }
        catch (DbUpdateException ex)
        {
            return BadRequest(ApiResponse<ProjectDto>.Fail("Project creation failed.", ex.InnerException?.Message ?? ex.Message));
        }
    }

    [HttpPut("{projectId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int projectId, ProjectRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var project = await _projectService.UpdateAsync(projectId, request, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<ProjectDto>.Ok(project, "Project updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ProjectDto>.Fail("Project update failed.", ex.Message));
        }
        catch (DbUpdateException ex)
        {
            return BadRequest(ApiResponse<ProjectDto>.Fail("Project update failed.", ex.InnerException?.Message ?? ex.Message));
        }
    }

    [HttpPatch("{projectId:int}/status")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateStatus(int projectId, ProjectStatusUpdateDto request, CancellationToken cancellationToken)
    {
        try
        {
            var project = await _projectService.UpdateStatusAsync(projectId, request, CurrentRole, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<ProjectDto>.Ok(project, "Project status updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ProjectDto>.Fail("Project status update failed.", ex.Message));
        }
    }

    [HttpDelete("{projectId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Cancel(int projectId, CancellationToken cancellationToken)
    {
        try
        {
            var project = await _projectService.CancelAsync(projectId, CurrentEmployeeId, cancellationToken);
            return Ok(ApiResponse<ProjectDto>.Ok(project, "Project cancelled successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ProjectDto>.Fail("Project cancellation failed.", ex.Message));
        }
    }

    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    private int CurrentEmployeeId => int.TryParse(User.FindFirstValue("employee_id"), out var employeeId) ? employeeId : 0;
}
