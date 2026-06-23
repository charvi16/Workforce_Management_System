using WMS.Application.DTOs.Leaves;
using WMS.Application.Common;

namespace WMS.Application.Interfaces;

public interface ILeaveService
{
    Task<IReadOnlyList<LeaveEmployeeDto>> GetAvailableEmployeesAsync(string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);

    Task<LeaveDto> ApplyAsync(ApplyLeaveRequestDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);

    Task<LeaveDto> CancelAsync(int leaveId, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);

    Task<LeaveDto> ReviewAsync(int leaveId, ReviewLeaveRequestDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);

    Task<PagedResult<LeaveDto>> GetLeavesAsync(int? employeeId, int? status, DateTime? fromDate, DateTime? toDate, string currentUserRole, int currentEmployeeId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);

    Task<LeaveStatisticsDto> GetStatisticsAsync(int? employeeId, int? year, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);
}
