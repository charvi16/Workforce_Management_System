using WMS.Application.DTOs.Reports;

namespace WMS.Application.Interfaces;

public interface IReportService
{
    Task<byte[]> GenerateAttendanceReportAsync(AttendanceReportRequestDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default);
}
