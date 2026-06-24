using Microsoft.Extensions.Logging.Abstractions;
using WMS.Application.DTOs.Departments;
using WMS.Infrastructure.Services;

namespace WMS.Tests;

public class DepartmentServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenDepartmentAlreadyExists_Throws()
    {
        await using var context = TestSupport.CreateContext();
        var service = new DepartmentService(context, NullLogger<DepartmentService>.Instance);

        await service.CreateAsync(new DepartmentRequestDto { DepartmentName = "Legal" });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new DepartmentRequestDto { DepartmentName = " legal " }));

        Assert.Equal("Department already exists.", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenEmployeesAssigned_Throws()
    {
        await using var context = TestSupport.CreateContext();
        await TestSupport.AddEmployeeAsync(context, employeeId: 1, departmentId: 10);
        var service = new DepartmentService(context, NullLogger<DepartmentService>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(10));

        Assert.Equal("Cannot delete a department that has assigned employees.", exception.Message);
    }
}
