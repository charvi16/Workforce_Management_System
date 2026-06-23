using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeDepartmentSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_FirstName' AND object_id = OBJECT_ID('dbo.Employees'))
BEGIN
    CREATE INDEX IX_Employees_FirstName ON dbo.Employees (FirstName);
END
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_LastName' AND object_id = OBJECT_ID('dbo.Employees'))
BEGIN
    CREATE INDEX IX_Employees_LastName ON dbo.Employees (LastName);
END
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_DepartmentId_RoleId' AND object_id = OBJECT_ID('dbo.Employees'))
BEGIN
    CREATE INDEX IX_Employees_DepartmentId_RoleId ON dbo.Employees (DepartmentId, RoleId);
END
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_DepartmentId_Status' AND object_id = OBJECT_ID('dbo.Employees'))
BEGIN
    CREATE INDEX IX_Employees_DepartmentId_Status ON dbo.Employees (DepartmentId, Status);
END
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Departments_DepartmentName' AND object_id = OBJECT_ID('dbo.Departments'))
BEGIN
    CREATE INDEX IX_Departments_DepartmentName ON dbo.Departments (DepartmentName);
END
""");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_RoleName' AND object_id = OBJECT_ID('dbo.Roles'))
BEGIN
    CREATE INDEX IX_Roles_RoleName ON dbo.Roles (RoleName);
END
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_RoleName' AND object_id = OBJECT_ID('dbo.Roles'))
BEGIN
    DROP INDEX IX_Roles_RoleName ON dbo.Roles;
END
""");

            migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Departments_DepartmentName' AND object_id = OBJECT_ID('dbo.Departments'))
BEGIN
    DROP INDEX IX_Departments_DepartmentName ON dbo.Departments;
END
""");

            migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_DepartmentId_Status' AND object_id = OBJECT_ID('dbo.Employees'))
BEGIN
    DROP INDEX IX_Employees_DepartmentId_Status ON dbo.Employees;
END
""");

            migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_DepartmentId_RoleId' AND object_id = OBJECT_ID('dbo.Employees'))
BEGIN
    DROP INDEX IX_Employees_DepartmentId_RoleId ON dbo.Employees;
END
""");

            migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_LastName' AND object_id = OBJECT_ID('dbo.Employees'))
BEGIN
    DROP INDEX IX_Employees_LastName ON dbo.Employees;
END
""");

            migrationBuilder.Sql("""
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_FirstName' AND object_id = OBJECT_ID('dbo.Employees'))
BEGIN
    DROP INDEX IX_Employees_FirstName ON dbo.Employees;
END
""");
        }
    }
}
