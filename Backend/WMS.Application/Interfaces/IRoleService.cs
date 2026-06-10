using WMS.Application.DTOs.Roles;

namespace WMS.Application.Interfaces;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
