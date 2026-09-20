using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM_Morcaffein.infrastructure.Migrations.TenantCrmDb
{
    /// <inheritdoc />
    public partial class AddFeedbackDecisionAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DecidedAt",
                table: "Feedbacks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DecidedBy",
                table: "Feedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionNotes",
                table: "Feedbacks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_DecidedBy",
                table: "Feedbacks",
                column: "DecidedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Employees_DecidedBy",
                table: "Feedbacks",
                column: "DecidedBy",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Employees_DecidedBy",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_DecidedBy",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "DecidedAt",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "DecidedBy",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "DecisionNotes",
                table: "Feedbacks");
        }
    }
}
