using Microsoft.Extensions.Logging.Abstractions;
using WMS.Application.DTOs.Attendance;
using WMS.Domain.Enums;
using WMS.Infrastructure.Services;

namespace WMS.Tests;

public class AttendanceServiceTests
{
    [Fact]
    public async Task CheckInAsync_WhenWorkModeIsInvalid_Throws()
    {
        await using var context = TestSupport.CreateContext();
        await TestSupport.AddEmployeeAsync(context, 1);
        var service = new AttendanceService(context, NullLogger<AttendanceService>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CheckInAsync(new CheckInRequestDto
            {
                EmployeeId = 1,
                WorkMode = 999
            }, nameof(UserRole.Employee), currentEmployeeId: 1));

        Assert.Equal("Invalid work mode selected.", exception.Message);
    }

    [Fact]
    public async Task CheckOutAsync_WhenNoOpenAttendance_Throws()
    {
        await using var context = TestSupport.CreateContext();
        await TestSupport.AddEmployeeAsync(context, 1);
        var service = new AttendanceService(context, NullLogger<AttendanceService>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CheckOutAsync(new CheckOutRequestDto { EmployeeId = 1 }, nameof(UserRole.Employee), currentEmployeeId: 1));

        Assert.Equal("Employee does not have an open attendance record.", exception.Message);
    }
}
