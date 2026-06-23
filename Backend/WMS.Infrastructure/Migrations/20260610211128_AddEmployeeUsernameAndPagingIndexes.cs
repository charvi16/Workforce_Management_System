using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeUsernameAndPagingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leaves_EmpId",
                table: "Leaves");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE e
                SET Username = u.Username
                FROM Employees e
                INNER JOIN UserLogins u ON u.EmployeeId = e.EmployeeId;
                """);

            migrationBuilder.Sql("""
                UPDATE Employees
                SET Username = CONCAT('employee', EmployeeId)
                WHERE Username IS NULL OR LTRIM(RTRIM(Username)) = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_EmpId_Status_FromDate",
                table: "Leaves",
                columns: new[] { "EmpId", "Status", "FromDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Username",
                table: "Employees",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_AttendanceDate_EmpId",
                table: "Attendances",
                columns: new[] { "AttendanceDate", "EmpId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leaves_EmpId_Status_FromDate",
                table: "Leaves");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Username",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_AttendanceDate_EmpId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_EmpId",
                table: "Leaves",
                column: "EmpId");
        }
    }
}
