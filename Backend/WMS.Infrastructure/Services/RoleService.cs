using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Roles;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly WmsDbContext _dbContext;

    public RoleService(WmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .OrderBy(r => r.RoleName)
            .Select(r => new RoleDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                Description = r.Description
            })
            .ToListAsync(cancellationToken);
    }
}
