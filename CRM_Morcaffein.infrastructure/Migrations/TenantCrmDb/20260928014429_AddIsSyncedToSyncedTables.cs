using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM_Morcaffein.infrastructure.Migrations.TenantCrmDb
{
    /// <inheritdoc />
    public partial class AddIsSyncedToSyncedTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "ProductSizes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "ProductModifiers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "PointLedgers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "PointAdjustmentRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "OrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "OrderItemModifiers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Feedbacks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Credentials",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Campaigns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "CampaignRedeems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Branches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // ---- Data fixup: mark all existing rows as already synced ----
            // Every row that exists at the moment this migration runs predates
            // the sync feature. They should NOT be pushed to cloud on first
            // worker run — the cloud already has equivalent data from its own
            // seed/migration path, and re-pushing everything would create a
            // mass upload with no benefit.
            //
            // New rows created AFTER this migration get IsSynced = false from
            // the C# default and will sync on the next worker tick.
            migrationBuilder.Sql("UPDATE Branches SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE Campaigns SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE CampaignRedeems SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE Credentials SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE Customers SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE Employees SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE Feedbacks SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE Orders SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE OrderItems SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE OrderItemModifiers SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE PointAdjustmentRequests SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE PointLedgers SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE Products SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE ProductModifiers SET IsSynced = 1;");
            migrationBuilder.Sql("UPDATE ProductSizes SET IsSynced = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "ProductSizes");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "ProductModifiers");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "PointLedgers");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "PointAdjustmentRequests");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "OrderItemModifiers");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "CampaignRedeems");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Branches");
        }
    }
}
