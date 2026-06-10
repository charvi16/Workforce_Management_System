using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Departments;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly WmsDbContext _dbContext;

    public DepartmentService(WmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Departments
            .Include(d => d.Employees)
            .OrderBy(d => d.DepartmentName)
            .Select(d => new DepartmentDto
            {
                DepartmentId = d.DepartmentId,
                DepartmentName = d.DepartmentName,
                Description = d.Description,
                EmployeeCount = d.Employees.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentDto> GetByIdAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.DepartmentId == departmentId, cancellationToken)
            ?? throw new InvalidOperationException("Department not found.");

        return MapToDto(department);
    }

    public async Task<DepartmentDto> CreateAsync(DepartmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var department = new Department
        {
            DepartmentName = request.DepartmentName.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(department.DepartmentId, cancellationToken);
    }

    public async Task<DepartmentDto> UpdateAsync(int departmentId, DepartmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentId == departmentId, cancellationToken)
            ?? throw new InvalidOperationException("Department not found.");

        department.DepartmentName = request.DepartmentName.Trim();
        department.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(departmentId, cancellationToken);
    }

    public async Task DeleteAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.DepartmentId == departmentId, cancellationToken)
            ?? throw new InvalidOperationException("Department not found.");

        if (department.Employees.Count > 0)
        {
            throw new InvalidOperationException("Cannot delete a department that has assigned employees.");
        }

        _dbContext.Departments.Remove(department);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DepartmentDto MapToDto(Department department)
    {
        return new DepartmentDto
        {
            DepartmentId = department.DepartmentId,
            DepartmentName = department.DepartmentName,
            Description = department.Description,
            EmployeeCount = department.Employees.Count
        };
    }
}
