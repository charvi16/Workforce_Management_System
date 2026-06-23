using WMS.Application.Common;
using WMS.Application.DTOs.Projects;

namespace WMS.Application.Interfaces;

public interface IProjectService
{
    Task<PagedResult<ProjectDto>> GetAllAsync(string currentUserRole, int currentEmployeeId, string? search, int? clientId, string? status, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<ProjectDto> GetByIdAsync(int projectId, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);
    Task<PagedResult<ProjectDto>> GetByClientAsync(int clientId, string currentUserRole, int currentEmployeeId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<ProjectDto> CreateAsync(ProjectRequestDto request, int currentEmployeeId, CancellationToken cancellationToken = default);
    Task<ProjectDto> UpdateAsync(int projectId, ProjectRequestDto request, int currentEmployeeId, CancellationToken cancellationToken = default);
    Task<ProjectDto> UpdateStatusAsync(int projectId, ProjectStatusUpdateDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);
    Task<ProjectDto> CancelAsync(int projectId, int currentEmployeeId, CancellationToken cancellationToken = default);
}
