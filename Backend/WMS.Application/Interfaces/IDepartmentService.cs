using WMS.Application.DTOs.Departments;
using WMS.Application.Common;

namespace WMS.Application.Interfaces;

public interface IDepartmentService
{
    Task<PagedResult<DepartmentDto>> SearchAsync(string? search, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentDto>> GetOptionsAsync(CancellationToken cancellationToken = default);
    Task<DepartmentDto> GetByIdAsync(int departmentId, CancellationToken cancellationToken = default);
    Task<DepartmentDto> CreateAsync(DepartmentRequestDto request, CancellationToken cancellationToken = default);
    Task<DepartmentDto> UpdateAsync(int departmentId, DepartmentRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int departmentId, CancellationToken cancellationToken = default);
}
