using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM_Morcaffein.infrastructure.Migrations.TenantCrmDb
{
    /// <inheritdoc />
    public partial class CampaignReasonField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Campaigns",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Campaigns");
        }
    }
}
