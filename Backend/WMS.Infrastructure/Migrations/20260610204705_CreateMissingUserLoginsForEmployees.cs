using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateMissingUserLoginsForEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO UserLogins (Username, PasswordHash, EmployeeId, RoleId, LastLogin)
                SELECT
                    e.Email,
                    'ahuKTmnXweTGiW0t67ORig==.YLQ1p4KtsCUtt4KY4BtkGz+y2CuXF3kjFMkf5WDY5jw=',
                    e.EmployeeId,
                    e.RoleId,
                    NULL
                FROM Employees e
                LEFT JOIN UserLogins u ON u.EmployeeId = e.EmployeeId
                WHERE u.UserId IS NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM UserLogins existing
                        WHERE existing.Username = e.Email
                    );
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE u
                FROM UserLogins u
                WHERE u.LastLogin IS NULL
                    AND u.PasswordHash = 'ahuKTmnXweTGiW0t67ORig==.YLQ1p4KtsCUtt4KY4BtkGz+y2CuXF3kjFMkf5WDY5jw='
                    AND EXISTS (
                        SELECT 1
                        FROM Employees e
                        WHERE e.EmployeeId = u.EmployeeId
                            AND e.Email = u.Username
                    );
                """);
        }
    }
}
