using WMS.Application.DTOs.Departments;

namespace WMS.Application.Interfaces;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DepartmentDto> GetByIdAsync(int departmentId, CancellationToken cancellationToken = default);
    Task<DepartmentDto> CreateAsync(DepartmentRequestDto request, CancellationToken cancellationToken = default);
    Task<DepartmentDto> UpdateAsync(int departmentId, DepartmentRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int departmentId, CancellationToken cancellationToken = default);
}
