using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Departments;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var departments = await _departmentService.SearchAsync(search, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<DepartmentDto>>.Ok(departments, "Departments retrieved successfully."));
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken cancellationToken)
    {
        var departments = await _departmentService.GetOptionsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DepartmentDto>>.Ok(departments, "Department options retrieved successfully."));
    }

    [HttpGet("{departmentId:int}")]
    public async Task<IActionResult> GetById(int departmentId, CancellationToken cancellationToken)
    {
        try
        {
            var department = await _departmentService.GetByIdAsync(departmentId, cancellationToken);
            return Ok(ApiResponse<DepartmentDto>.Ok(department, "Department retrieved successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<DepartmentDto>.Fail("Department lookup failed.", ex.Message));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(DepartmentRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var department = await _departmentService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { departmentId = department.DepartmentId }, ApiResponse<DepartmentDto>.Ok(department, "Department created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<DepartmentDto>.Fail("Department creation failed.", ex.Message));
        }
    }

    [HttpPut("{departmentId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int departmentId, DepartmentRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var department = await _departmentService.UpdateAsync(departmentId, request, cancellationToken);
            return Ok(ApiResponse<DepartmentDto>.Ok(department, "Department updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<DepartmentDto>.Fail("Department update failed.", ex.Message));
        }
    }

    [HttpDelete("{departmentId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int departmentId, CancellationToken cancellationToken)
    {
        try
        {
            await _departmentService.DeleteAsync(departmentId, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { departmentId }, "Department deleted successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("Department delete failed.", ex.Message));
        }
    }
}
