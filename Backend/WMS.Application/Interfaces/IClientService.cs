using WMS.Application.Common;
using WMS.Application.DTOs.Clients;

namespace WMS.Application.Interfaces;

public interface IClientService
{
    Task<PagedResult<ClientDto>> GetAllAsync(string currentUserRole, int currentEmployeeId, string? search, bool? status, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<ClientDto> GetByIdAsync(int clientId, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);
    Task<ClientDto> CreateAsync(ClientRequestDto request, int currentEmployeeId, CancellationToken cancellationToken = default);
    Task<ClientDto> UpdateAsync(int clientId, ClientRequestDto request, int currentEmployeeId, CancellationToken cancellationToken = default);
    Task<ClientDto> DeactivateAsync(int clientId, int currentEmployeeId, CancellationToken cancellationToken = default);
}
