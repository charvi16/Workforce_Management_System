using Microsoft.Extensions.Logging.Abstractions;
using WMS.Application.DTOs.Leaves;
using WMS.Domain.Enums;
using WMS.Infrastructure.Services;

namespace WMS.Tests;

public class LeaveServiceTests
{
    [Fact]
    public async Task ApplyAsync_WhenLeaveOverlapsExistingPendingLeave_Throws()
    {
        await using var context = TestSupport.CreateContext();
        await TestSupport.AddEmployeeAsync(context, 1);
        var service = new LeaveService(context, NullLogger<LeaveService>.Instance);
        var fromDate = DateTime.UtcNow.Date.AddDays(3);
        var toDate = fromDate.AddDays(2);

        await service.ApplyAsync(new ApplyLeaveRequestDto
        {
            EmployeeId = 1,
            LeaveType = (int)LeaveType.Sick,
            FromDate = fromDate,
            ToDate = toDate
        }, nameof(UserRole.Employee), currentEmployeeId: 1);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApplyAsync(new ApplyLeaveRequestDto
            {
                EmployeeId = 1,
                LeaveType = (int)LeaveType.Casual,
                FromDate = fromDate.AddDays(1),
                ToDate = toDate.AddDays(1)
            }, nameof(UserRole.Employee), currentEmployeeId: 1));

        Assert.Equal("A pending or approved leave already exists for the selected date range.", exception.Message);
    }

    [Fact]
    public async Task ReviewAsync_WhenReviewerOwnsLeave_Throws()
    {
        await using var context = TestSupport.CreateContext();
        await TestSupport.AddEmployeeAsync(context, 1, nameof(UserRole.Manager));
        var service = new LeaveService(context, NullLogger<LeaveService>.Instance);
        var leave = await service.ApplyAsync(new ApplyLeaveRequestDto
        {
            EmployeeId = 1,
            LeaveType = (int)LeaveType.Sick,
            FromDate = DateTime.UtcNow.Date.AddDays(5),
            ToDate = DateTime.UtcNow.Date.AddDays(6)
        }, nameof(UserRole.Manager), currentEmployeeId: 1);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReviewAsync(leave.LeaveId, new ReviewLeaveRequestDto { IsApproved = true }, nameof(UserRole.Manager), currentEmployeeId: 1));

        Assert.Equal("Users cannot approve or reject their own leave requests.", exception.Message);
    }
}
