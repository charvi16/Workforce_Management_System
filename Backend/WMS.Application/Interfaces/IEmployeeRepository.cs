using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<IReadOnlyList<Employee>> SearchAsync(string? search, int? departmentId, int? roleId, CancellationToken cancellationToken = default);
    Task<Employee?> GetByIdAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, int? excludeEmployeeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    void Update(Employee employee);
    void Delete(Employee employee);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
