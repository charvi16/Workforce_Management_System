using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairClientAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF COL_LENGTH('dbo.Clients', 'CreatedOn') IS NULL
BEGIN
    ALTER TABLE dbo.Clients
    ADD CreatedOn datetime2 NOT NULL CONSTRAINT DF_Clients_CreatedOn DEFAULT (GETDATE());
END
""");

            migrationBuilder.Sql("""
IF COL_LENGTH('dbo.Clients', 'UpdatedOn') IS NULL
BEGIN
    ALTER TABLE dbo.Clients
    ADD UpdatedOn datetime2 NULL;
END
""");

            migrationBuilder.Sql("""
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('dbo.Clients')
      AND c.name = 'ClientPhoneNumber'
      AND t.name NOT IN ('varchar', 'nvarchar', 'char', 'nchar')
)
BEGIN
    ALTER TABLE dbo.Clients ALTER COLUMN ClientPhoneNumber varchar(15) NULL;
END
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF COL_LENGTH('dbo.Clients', 'UpdatedOn') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Clients DROP COLUMN UpdatedOn;
END
""");

            migrationBuilder.Sql("""
IF COL_LENGTH('dbo.Clients', 'CreatedOn') IS NOT NULL
BEGIN
    DECLARE @constraintName sysname;
    SELECT @constraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Clients')
      AND c.name = 'CreatedOn';

    IF @constraintName IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE dbo.Clients DROP CONSTRAINT ' + QUOTENAME(@constraintName));
    END

    ALTER TABLE dbo.Clients DROP COLUMN CreatedOn;
END
""");
        }
    }
}
