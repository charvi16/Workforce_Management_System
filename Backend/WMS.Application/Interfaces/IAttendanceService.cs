using WMS.Application.DTOs.Attendance;
using WMS.Application.Common;

namespace WMS.Application.Interfaces;

public interface IAttendanceService
{
    Task<IReadOnlyList<AttendanceEmployeeDto>> GetAvailableEmployeesAsync(string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);

    Task<AttendanceDto> CheckInAsync(CheckInRequestDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);

    Task<AttendanceDto> CheckOutAsync(CheckOutRequestDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);

    Task<PagedResult<AttendanceDto>> GetMonthlyAttendanceAsync(int? employeeId, int? departmentId, int month, int year, string currentUserRole, int currentEmployeeId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
}
