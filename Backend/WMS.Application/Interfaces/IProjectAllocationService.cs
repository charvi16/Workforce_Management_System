using WMS.Application.Common;
using WMS.Application.DTOs.ProjectAllocations;

namespace WMS.Application.Interfaces;

public interface IProjectAllocationService
{
    Task<PagedResult<ProjectAllocationDto>> GetAllAsync(string currentUserRole, int currentEmployeeId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<PagedResult<ProjectAllocationDto>> GetByProjectAsync(int projectId, string currentUserRole, int currentEmployeeId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<PagedResult<ProjectAllocationDto>> GetByEmployeeAsync(int employeeId, string currentUserRole, int currentEmployeeId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<ProjectAllocationDto> CreateAsync(ProjectAllocationRequestDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);
    Task<ProjectAllocationDto> UpdateAsync(int allocationId, ProjectAllocationRequestDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);
    Task<ProjectAllocationDto> DeleteAsync(int allocationId, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);
}
