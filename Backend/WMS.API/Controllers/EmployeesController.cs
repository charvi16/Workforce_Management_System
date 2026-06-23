using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Employees;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] int? departmentId,
        [FromQuery] int? roleId,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var employees = await _employeeService.SearchAsync(search, departmentId, roleId, role, status, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<WMS.Application.Common.PagedResult<EmployeeDto>>.Ok(employees, "Employees retrieved successfully."));
    }

    [HttpGet("{employeeId:int}")]
    public async Task<IActionResult> GetById(int employeeId, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeService.GetByIdAsync(employeeId, cancellationToken);
            return Ok(ApiResponse<EmployeeDto>.Ok(employee, "Employee retrieved successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<EmployeeDto>.Fail("Employee lookup failed.", ex.Message));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateEmployeeRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { employeeId = employee.EmployeeId }, ApiResponse<EmployeeDto>.Ok(employee, "Employee created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<EmployeeDto>.Fail("Employee creation failed.", ex.Message));
        }
    }

    [HttpPut("{employeeId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int employeeId, UpdateEmployeeRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeService.UpdateAsync(employeeId, request, cancellationToken);
            return Ok(ApiResponse<EmployeeDto>.Ok(employee, "Employee updated successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<EmployeeDto>.Fail("Employee update failed.", ex.Message));
        }
    }

    [HttpDelete("{employeeId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int employeeId, CancellationToken cancellationToken)
    {
        try
        {
            await _employeeService.DeleteAsync(employeeId, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { employeeId }, "Employee deleted successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail("Employee delete failed.", ex.Message));
        }
    }

    [HttpPut("{employeeId:int}/department")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignDepartment(int employeeId, AssignDepartmentRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeService.AssignDepartmentAsync(employeeId, request.DepartmentId, cancellationToken);
            return Ok(ApiResponse<EmployeeDto>.Ok(employee, "Employee department assigned successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<EmployeeDto>.Fail("Department assignment failed.", ex.Message));
        }
    }

    [HttpPut("{employeeId:int}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole(int employeeId, AssignRoleRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var employee = await _employeeService.AssignRoleAsync(employeeId, request.RoleId, cancellationToken);
            return Ok(ApiResponse<EmployeeDto>.Ok(employee, "Employee role assigned successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<EmployeeDto>.Fail("Role assignment failed.", ex.Message));
        }
    }
}
