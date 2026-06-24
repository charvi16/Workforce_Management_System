using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WMS.Application.DTOs.Projects;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.Services;

namespace WMS.Tests;

public class ProjectServiceTests
{
    [Fact]
    public async Task CreateAsync_WithMemberIds_CreatesActiveAllocationsAndMemberCount()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context, 1, 2);
        var service = new ProjectService(context);

        var project = await service.CreateAsync(new ProjectRequestDto
        {
            ProjectName = "Workforce Portal",
            Status = "Planned",
            StartDate = DateTime.UtcNow.Date,
            MemberIds = new[] { 1, 2 }
        }, currentEmployeeId: 99);

        Assert.Equal(2, project.MembersCount);

        var allocations = await context.EmployeeProjectAllocations
            .Where(a => a.ProjectId == project.ProjectId)
            .OrderBy(a => a.EmpId)
            .ToListAsync();

        Assert.Equal(new[] { 1, 2 }, allocations.Select(a => a.EmpId).ToArray());
        Assert.All(allocations, allocation => Assert.True(allocation.Status));
    }

    [Fact]
    public async Task UpdateAsync_WhenMemberIdsChange_DeactivatesRemovedMembersAndAddsNewMembers()
    {
        await using var context = CreateContext();
        await SeedEmployeesAsync(context, 1, 2, 3);
        var service = new ProjectService(context);

        var created = await service.CreateAsync(new ProjectRequestDto
        {
            ProjectName = "Workforce Portal",
            Status = "Planned",
            StartDate = DateTime.UtcNow.Date,
            MemberIds = new[] { 1, 2 }
        }, currentEmployeeId: 99);

        var updated = await service.UpdateAsync(created.ProjectId, new ProjectRequestDto
        {
            ProjectName = "Workforce Portal Updated",
            Status = "Active",
            StartDate = DateTime.UtcNow.Date,
            MemberIds = new[] { 2, 3 }
        }, currentEmployeeId: 99);

        Assert.Equal("Workforce Portal Updated", updated.ProjectName);
        Assert.Equal(2, updated.MembersCount);

        var allocations = await context.EmployeeProjectAllocations
            .Where(a => a.ProjectId == created.ProjectId)
            .ToListAsync();

        Assert.False(allocations.Single(a => a.EmpId == 1).Status);
        Assert.True(allocations.Single(a => a.EmpId == 2).Status);
        Assert.True(allocations.Single(a => a.EmpId == 3).Status);
    }

    private static WmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new WmsDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedEmployeesAsync(WmsDbContext context, params int[] employeeIds)
    {
        context.Departments.Add(new Department
        {
            DepartmentId = 10,
            DepartmentName = "Engineering"
        });

        context.Roles.Add(new Role
        {
            RoleId = 10,
            RoleName = nameof(UserRole.Employee)
        });

        foreach (var employeeId in employeeIds)
        {
            context.Employees.Add(new Employee
            {
                EmployeeId = employeeId,
                Username = $"employee{employeeId}",
                FirstName = $"Employee{employeeId}",
                LastName = "User",
                Email = $"employee{employeeId}@example.com",
                PhoneNumber = $"555000{employeeId:D4}",
                Gender = Gender.Other,
                DOB = new DateTime(1995, 1, 1),
                DOJ = new DateTime(2024, 1, 1),
                DepartmentId = 10,
                RoleId = 10,
                Status = EmployeeStatus.Active
            });
        }

        await context.SaveChangesAsync();
    }
}
