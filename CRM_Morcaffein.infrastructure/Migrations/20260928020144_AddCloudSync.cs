using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM_Morcaffein.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "TermsAndConditions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "SuperAdmins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Subscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Plans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "CompanyUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CloudDatabaseName",
                table: "CompanyDatabases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CloudServerName",
                table: "CompanyDatabases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "CompanyDatabases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // The WHERE clause includes "ServerName LIKE '(localdb)%'" so this
            // only runs on the local master. When this same migration runs
            // against the cloud master, its CompanyDatabases rows have real
            // hostnames (db67068.public.databaseasp.net), not (localdb)...,
            // so the guard is false and the UPDATE is a no-op. Cloud doesn't
            // point at itself.
            migrationBuilder.Sql(
                "UPDATE CompanyDatabases SET " +
                "  CloudServerName = 'db67068.public.databaseasp.net', " +
                "  CloudDatabaseName = 'db67068' " +
                "WHERE CompanyId = 1 AND ServerName LIKE '(localdb)%';");

            migrationBuilder.Sql(
                "UPDATE CompanyDatabases SET " +
                "  CloudServerName = 'db67071.public.databaseasp.net', " +
                "  CloudDatabaseName = 'db67071' " +
                "WHERE CompanyId = 2 AND ServerName LIKE '(localdb)%';");

            migrationBuilder.Sql(
                "UPDATE CompanyDatabases SET " +
                "  CloudServerName = 'db67072.public.databaseasp.net', " +
                "  CloudDatabaseName = 'db67072' " +
                "WHERE CompanyId = 3 AND ServerName LIKE '(localdb)%';");

            // ---- Data fixup: mark all existing master rows as already synced ----
            // Every row that exists at the moment this migration runs predates the
            // sync feature. The cloud master already has equivalent data (seeded
            // independently). Re-pushing everything would be noise.
            migrationBuilder.Sql("UPDATE Companies SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE Subscriptions SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE CompanyUsers SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE SuperAdmins SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE Plans SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE TermsAndConditions SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE CompanyDatabases SET IsSynced = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "TermsAndConditions");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "SuperAdmins");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "CompanyUsers");

            migrationBuilder.DropColumn(
                name: "CloudDatabaseName",
                table: "CompanyDatabases");

            migrationBuilder.DropColumn(
                name: "CloudServerName",
                table: "CompanyDatabases");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "CompanyDatabases");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Companies");
        }
    }
}
