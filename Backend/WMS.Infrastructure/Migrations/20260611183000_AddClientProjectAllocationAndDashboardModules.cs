using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientProjectAllocationAndDashboardModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_AttendanceDate_EmpId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProjectAllocations_EmpId",
                table: "EmployeeProjectAllocations");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProjectAllocations_ProjectId",
                table: "EmployeeProjectAllocations");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Clients",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<DateTime?>(
                name: "UpdatedOn",
                table: "Clients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClientAddress",
                table: "Clients",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClientLocation",
                table: "Clients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClientPhoneNumber",
                table: "Clients",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,0)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Projects",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<DateTime?>(
                name: "UpdatedOn",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Projects");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Projects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Planned");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Projects",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Projects",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "EmployeeProjectAllocations");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "EmployeeProjectAllocations");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "EmployeeProjectAllocations");

            migrationBuilder.DropColumn(
                name: "CreateDate",
                table: "EmployeeProjectAllocations");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "EmployeeProjectAllocations",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "EmployeeProjectAllocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime?>(
                name: "UpdatedOn",
                table: "EmployeeProjectAllocations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int?>(
                name: "UpdatedBy",
                table: "EmployeeProjectAllocations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleInProject",
                table: "EmployeeProjectAllocations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int?>(
                name: "AllocationPercentage",
                table: "EmployeeProjectAllocations",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "AssignedOn",
                table: "EmployeeProjectAllocations",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmpId_AttendanceDate_CheckIn_CheckOut",
                table: "Attendances",
                columns: new[] { "EmpId", "AttendanceDate", "CheckIn", "CheckOut" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_ClientName",
                table: "Clients",
                column: "ClientName");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Status",
                table: "Clients",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_RoleId",
                table: "Employees",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Status",
                table: "Employees",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProjectAllocations_EmpId_ProjectId_Status",
                table: "EmployeeProjectAllocations",
                columns: new[] { "EmpId", "ProjectId", "Status" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProjectAllocations_ProjectId",
                table: "EmployeeProjectAllocations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ClientId",
                table: "Projects",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_EndDate",
                table: "Projects",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_StartDate",
                table: "Projects",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status",
                table: "Projects",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_EmpId_AttendanceDate_CheckIn_CheckOut",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Clients_ClientName",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_Status",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_RoleId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Status",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProjectAllocations_EmpId_ProjectId_Status",
                table: "EmployeeProjectAllocations");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProjectAllocations_ProjectId",
                table: "EmployeeProjectAllocations");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ClientId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_EndDate",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_StartDate",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Status",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Clients");

            migrationBuilder.AlterColumn<string>(
                name: "ClientAddress",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClientLocation",
                table: "Clients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ClientPhoneNumber",
                table: "Clients",
                type: "numeric(10,0)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Projects");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Projects",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Projects",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "EmployeeProjectAllocations");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "EmployeeProjectAllocations");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "EmployeeProjectAllocations");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "EmployeeProjectAllocations");

            migrationBuilder.DropColumn(
                name: "RoleInProject",
                table: "EmployeeProjectAllocations");

            migrationBuilder.DropColumn(
                name: "AllocationPercentage",
                table: "EmployeeProjectAllocations");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AssignedOn",
                table: "EmployeeProjectAllocations",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDate",
                table: "EmployeeProjectAllocations",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "EmployeeProjectAllocations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "EmployeeProjectAllocations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime?>(
                name: "UpdatedDate",
                table: "EmployeeProjectAllocations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_AttendanceDate_EmpId",
                table: "Attendances",
                columns: new[] { "AttendanceDate", "EmpId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProjectAllocations_EmpId",
                table: "EmployeeProjectAllocations",
                column: "EmpId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProjectAllocations_ProjectId",
                table: "EmployeeProjectAllocations",
                column: "ProjectId");
        }
    }
}
