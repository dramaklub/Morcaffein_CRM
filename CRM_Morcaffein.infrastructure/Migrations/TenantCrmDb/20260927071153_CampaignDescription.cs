using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM_Morcaffein.infrastructure.Migrations.TenantCrmDb
{
    /// <inheritdoc />
    public partial class CampaignDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Campaigns",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Campaigns");
        }
    }
}
