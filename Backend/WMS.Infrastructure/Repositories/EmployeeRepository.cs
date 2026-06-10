using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly WmsDbContext _dbContext;

    public EmployeeRepository(WmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Employee>> SearchAsync(string? search, int? departmentId, int? roleId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var likeTerm = $"%{EscapeLikePattern(term)}%";
            var status = ParseStatus(term);
            var employeeId = int.TryParse(term, out var parsedEmployeeId)
                ? parsedEmployeeId
                : (int?)null;

            query = query.Where(e =>
                EF.Functions.Like(e.FirstName, likeTerm, @"\") ||
                EF.Functions.Like(e.LastName, likeTerm, @"\") ||
                EF.Functions.Like(e.FirstName + " " + e.LastName, likeTerm, @"\") ||
                EF.Functions.Like(e.LastName + " " + e.FirstName, likeTerm, @"\") ||
                EF.Functions.Like(e.Email, likeTerm, @"\") ||
                EF.Functions.Like(e.PhoneNumber, likeTerm, @"\") ||
                EF.Functions.Like(e.Department.DepartmentName, likeTerm, @"\") ||
                EF.Functions.Like(e.Role.RoleName, likeTerm, @"\") ||
                (employeeId.HasValue && e.EmployeeId == employeeId.Value) ||
                (status.HasValue && e.Status == status.Value));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (roleId.HasValue)
        {
            query = query.Where(e => e.RoleId == roleId.Value);
        }

        return await query
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync(cancellationToken);
    }

    public Task<Employee?> GetByIdAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Employees
            .Include(e => e.Department)
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, int? excludeEmployeeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();
        return _dbContext.Employees.AnyAsync(e =>
            e.Email == normalizedEmail &&
            (!excludeEmployeeId.HasValue || e.EmployeeId != excludeEmployeeId.Value),
            cancellationToken);
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await _dbContext.Employees.AddAsync(employee, cancellationToken);
    }

    public void Update(Employee employee)
    {
        _dbContext.Employees.Update(employee);
    }

    public void Delete(Employee employee)
    {
        _dbContext.Employees.Remove(employee);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_")
            .Replace("[", @"\[");
    }

    private static EmployeeStatus? ParseStatus(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "active" => EmployeeStatus.Active,
            "inactive" => EmployeeStatus.Inactive,
            _ => null
        };
    }
}
