using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Tests;

internal static class TestSupport
{
    public static WmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new WmsDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static async Task<Employee> AddEmployeeAsync(
        WmsDbContext context,
        int employeeId,
        string roleName = nameof(UserRole.Employee),
        int departmentId = 10,
        EmployeeStatus status = EmployeeStatus.Active)
    {
        if (!await context.Departments.AnyAsync(d => d.DepartmentId == departmentId))
        {
            context.Departments.Add(new Department
            {
                DepartmentId = departmentId,
                DepartmentName = $"Department {departmentId}"
            });
        }

        var roleId = roleName.ToLowerInvariant() switch
        {
            "admin" => 100,
            "manager" => 101,
            _ => 102
        };

        if (!await context.Roles.AnyAsync(r => r.RoleId == roleId))
        {
            context.Roles.Add(new Role
            {
                RoleId = roleId,
                RoleName = roleName
            });
        }

        var employee = new Employee
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
            DepartmentId = departmentId,
            RoleId = roleId,
            Status = status
        };

        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        return employee;
    }
}
