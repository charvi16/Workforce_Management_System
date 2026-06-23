using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkUserLoginToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('UserLogins', 'EmployeeId') IS NULL
                BEGIN
                    ALTER TABLE UserLogins ADD EmployeeId int NULL;
                END
                """);

            migrationBuilder.Sql("""
                UPDATE u
                SET EmployeeId = e.EmployeeId
                FROM UserLogins u
                INNER JOIN Employees e ON u.Username = e.Email;
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM UserLogins u
                    LEFT JOIN Employees e ON u.EmployeeId = e.EmployeeId
                    WHERE u.EmployeeId IS NOT NULL
                        AND e.EmployeeId IS NULL
                )
                BEGIN
                    THROW 51000, 'Cannot link UserLogins to Employees. A UserLogin.EmployeeId points to a missing employee.', 1;
                END
                """);

            migrationBuilder.Sql("""
                INSERT INTO Employees (
                    FirstName,
                    LastName,
                    Email,
                    PhoneNumber,
                    Gender,
                    DOB,
                    DOJ,
                    DepartmentId,
                    RoleId,
                    Status,
                    CreatedOn
                )
                SELECT
                    LEFT(NULLIF(LTRIM(RTRIM(u.Username)), ''), 50),
                    'User',
                    CONCAT('userlogin-', u.UserId, '@wms.local'),
                    '0000000000',
                    3,
                    '1990-01-01',
                    CONVERT(date, SYSUTCDATETIME()),
                    COALESCE((SELECT TOP 1 DepartmentId FROM Departments ORDER BY DepartmentId), 1),
                    u.RoleId,
                    1,
                    SYSUTCDATETIME()
                FROM UserLogins u
                WHERE u.EmployeeId IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE u
                SET EmployeeId = e.EmployeeId
                FROM UserLogins u
                INNER JOIN Employees e ON e.Email = CONCAT('userlogin-', u.UserId, '@wms.local')
                WHERE u.EmployeeId IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE e
                SET RoleId = u.RoleId
                FROM Employees e
                INNER JOIN UserLogins u ON u.EmployeeId = e.EmployeeId
                WHERE e.RoleId <> u.RoleId;
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM UserLogins
                    WHERE EmployeeId IS NULL
                )
                BEGIN
                    THROW 51001, 'Cannot link UserLogins to Employees. One or more logins could not be assigned an employee profile.', 1;
                END
                """);

            migrationBuilder.Sql("ALTER TABLE UserLogins ALTER COLUMN EmployeeId int NOT NULL;");

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_UserLogins_EmployeeId'
                        AND object_id = OBJECT_ID('UserLogins')
                )
                BEGIN
                    CREATE UNIQUE INDEX IX_UserLogins_EmployeeId ON UserLogins(EmployeeId);
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_UserLogins_Employees_EmployeeId'
                )
                BEGIN
                    ALTER TABLE UserLogins
                    ADD CONSTRAINT FK_UserLogins_Employees_EmployeeId
                    FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId)
                    ON DELETE NO ACTION;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_UserLogins_Employees_EmployeeId'
                )
                BEGIN
                    ALTER TABLE UserLogins DROP CONSTRAINT FK_UserLogins_Employees_EmployeeId;
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_UserLogins_EmployeeId'
                        AND object_id = OBJECT_ID('UserLogins')
                )
                BEGIN
                    DROP INDEX IX_UserLogins_EmployeeId ON UserLogins;
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('UserLogins', 'EmployeeId') IS NOT NULL
                BEGIN
                    ALTER TABLE UserLogins DROP COLUMN EmployeeId;
                END
                """);
        }
    }
}
