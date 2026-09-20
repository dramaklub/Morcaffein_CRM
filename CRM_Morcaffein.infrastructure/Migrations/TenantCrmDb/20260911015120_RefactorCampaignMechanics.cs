using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM_Morcaffein.infrastructure.Migrations.TenantCrmDb
{
    /// <inheritdoc />
    public partial class RefactorCampaignMechanics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountAmount",
                table: "Campaigns",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRedemptionsPerCustomer",
                table: "Campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MinPurchaseAmount",
                table: "Campaigns",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredProductId",
                table: "Campaigns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RewardProductId",
                table: "Campaigns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetTier",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_RequiredProductId",
                table: "Campaigns",
                column: "RequiredProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_RewardProductId",
                table: "Campaigns",
                column: "RewardProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Products_RequiredProductId",
                table: "Campaigns",
                column: "RequiredProductId",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Products_RewardProductId",
                table: "Campaigns",
                column: "RewardProductId",
                principalTable: "Products",
                principalColumn: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_Products_RequiredProductId",
                table: "Campaigns");

            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_Products_RewardProductId",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_RequiredProductId",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_RewardProductId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "MaxDiscountAmount",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "MaxRedemptionsPerCustomer",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "MinPurchaseAmount",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "RequiredProductId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "RewardProductId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "TargetTier",
                table: "Campaigns");
        }
    }
}
