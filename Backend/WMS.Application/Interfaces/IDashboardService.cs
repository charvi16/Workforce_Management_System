using WMS.Application.DTOs.Dashboard;

namespace WMS.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponseDto> GetAdminDashboardAsync(int currentEmployeeId, CancellationToken cancellationToken = default);
    Task<DashboardResponseDto> GetManagerDashboardAsync(int currentEmployeeId, CancellationToken cancellationToken = default);
    Task<DashboardResponseDto> GetEmployeeDashboardAsync(int currentEmployeeId, CancellationToken cancellationToken = default);
}
