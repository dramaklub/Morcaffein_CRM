using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM_Morcaffein.infrastructure.Migrations.TenantCrmDb
{
    /// <inheritdoc />
    public partial class CampaignDecisionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_Employees_ApprovedBy",
                table: "Campaigns");

            migrationBuilder.RenameColumn(
                name: "ApprovedBy",
                table: "Campaigns",
                newName: "DecidedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Campaigns_ApprovedBy",
                table: "Campaigns",
                newName: "IX_Campaigns_DecidedBy");

            migrationBuilder.AddColumn<DateTime>(
                name: "DecidedAt",
                table: "Campaigns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionNotes",
                table: "Campaigns",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Employees_DecidedBy",
                table: "Campaigns",
                column: "DecidedBy",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_Employees_DecidedBy",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "DecidedAt",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "DecisionNotes",
                table: "Campaigns");

            migrationBuilder.RenameColumn(
                name: "DecidedBy",
                table: "Campaigns",
                newName: "ApprovedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Campaigns_DecidedBy",
                table: "Campaigns",
                newName: "IX_Campaigns_ApprovedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Employees_ApprovedBy",
                table: "Campaigns",
                column: "ApprovedBy",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
