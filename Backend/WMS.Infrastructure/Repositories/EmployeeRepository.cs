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

    public Task<bool> UsernameExistsAsync(string username, int? excludeEmployeeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim();
        return _dbContext.Employees.AnyAsync(e =>
            e.Username == normalizedUsername &&
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

}
