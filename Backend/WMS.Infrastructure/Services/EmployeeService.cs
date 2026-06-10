using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Employees;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly WmsDbContext _dbContext;

    public EmployeeService(IEmployeeRepository employeeRepository, WmsDbContext dbContext)
    {
        _employeeRepository = employeeRepository;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<EmployeeDto>> SearchAsync(string? search, int? departmentId, int? roleId, CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.SearchAsync(search, departmentId, roleId, cancellationToken);
        return employees.Select(MapToDto).ToList();
    }

    public async Task<EmployeeDto> GetByIdAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrowAsync(employeeId, cancellationToken);
        return MapToDto(employee);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateReferencesAsync(request.DepartmentId, request.RoleId, cancellationToken);

        if (await _employeeRepository.EmailExistsAsync(request.Email, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("Employee email already exists.");
        }

        var employee = new Employee();
        Apply(employee, request);

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employee.EmployeeId, cancellationToken);
    }

    public async Task<EmployeeDto> UpdateAsync(int employeeId, UpdateEmployeeRequestDto request, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrowAsync(employeeId, cancellationToken);
        await ValidateReferencesAsync(request.DepartmentId, request.RoleId, cancellationToken);

        if (await _employeeRepository.EmailExistsAsync(request.Email, employeeId, cancellationToken))
        {
            throw new InvalidOperationException("Employee email already exists.");
        }

        Apply(employee, request);
        employee.UpdatedOn = DateTime.UtcNow;

        _employeeRepository.Update(employee);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employeeId, cancellationToken);
    }

    public async Task DeleteAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrowAsync(employeeId, cancellationToken);
        _employeeRepository.Delete(employee);
        await _employeeRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmployeeDto> AssignDepartmentAsync(int employeeId, int departmentId, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrowAsync(employeeId, cancellationToken);
        if (!await _dbContext.Departments.AnyAsync(d => d.DepartmentId == departmentId, cancellationToken))
        {
            throw new InvalidOperationException("Invalid department selected.");
        }

        employee.DepartmentId = departmentId;
        employee.UpdatedOn = DateTime.UtcNow;
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employeeId, cancellationToken);
    }

    public async Task<EmployeeDto> AssignRoleAsync(int employeeId, int roleId, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrowAsync(employeeId, cancellationToken);
        if (!await _dbContext.Roles.AnyAsync(r => r.RoleId == roleId, cancellationToken))
        {
            throw new InvalidOperationException("Invalid role selected.");
        }

        employee.RoleId = roleId;
        employee.UpdatedOn = DateTime.UtcNow;
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employeeId, cancellationToken);
    }

    private async Task<Employee> GetEmployeeOrThrowAsync(int employeeId, CancellationToken cancellationToken)
    {
        return await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new InvalidOperationException("Employee not found.");
    }

    private async Task ValidateReferencesAsync(int departmentId, int roleId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Departments.AnyAsync(d => d.DepartmentId == departmentId, cancellationToken))
        {
            throw new InvalidOperationException("Invalid department selected.");
        }

        if (!await _dbContext.Roles.AnyAsync(r => r.RoleId == roleId, cancellationToken))
        {
            throw new InvalidOperationException("Invalid role selected.");
        }
    }

    private static void Apply(Employee employee, CreateEmployeeRequestDto request)
    {
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Email = request.Email.Trim();
        employee.PhoneNumber = request.PhoneNumber.Trim();
        employee.Gender = (Gender)request.Gender;
        employee.DOB = request.DOB;
        employee.DOJ = request.DOJ;
        employee.DepartmentId = request.DepartmentId;
        employee.RoleId = request.RoleId;
        employee.Status = (EmployeeStatus)request.Status;
    }

    private static EmployeeDto MapToDto(Employee employee)
    {
        return new EmployeeDto
        {
            EmployeeId = employee.EmployeeId,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            Gender = (int)employee.Gender,
            GenderName = employee.Gender.ToString(),
            DOB = employee.DOB,
            DOJ = employee.DOJ,
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department.DepartmentName,
            RoleId = employee.RoleId,
            RoleName = employee.Role.RoleName,
            Status = (int)employee.Status,
            StatusName = employee.Status.ToString()
        };
    }
}
