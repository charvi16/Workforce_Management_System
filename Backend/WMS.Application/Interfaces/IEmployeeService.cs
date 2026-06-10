using WMS.Application.DTOs.Employees;

namespace WMS.Application.Interfaces;

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeDto>> SearchAsync(string? search, int? departmentId, int? roleId, CancellationToken cancellationToken = default);
    Task<EmployeeDto> GetByIdAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<EmployeeDto> CreateAsync(CreateEmployeeRequestDto request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> UpdateAsync(int employeeId, UpdateEmployeeRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<EmployeeDto> AssignDepartmentAsync(int employeeId, int departmentId, CancellationToken cancellationToken = default);
    Task<EmployeeDto> AssignRoleAsync(int employeeId, int roleId, CancellationToken cancellationToken = default);
}
