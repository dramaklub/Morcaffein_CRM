using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM_Morcaffein.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanAndCompanySubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentPlanId",
                table: "Companies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ModulesCsv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                                constraints: table =>
                                {
                                    table.PrimaryKey("PK_Plans", x => x.PlanId);
                                });

            // Seed the three subscription plans.
            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "Name", "Description", "ModulesCsv", "MonthlyPrice", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { "Basic",    "Transactions and customer data collection only.",
                      "DataCollection,Transactions", 999m, true, DateTime.UtcNow, DateTime.UtcNow },
                    { "Standard", "Everything in Basic plus feedback, retention campaigns, and business intelligence.",
                      "DataCollection,Transactions,Feedback,Retention,BI", 1999m, true, DateTime.UtcNow, DateTime.UtcNow },
                    { "Full",     "Everything in Standard plus multi-branch management.",
                      "DataCollection,Transactions,Feedback,Retention,BI,BranchManagement", 2999m, true, DateTime.UtcNow, DateTime.UtcNow },
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CurrentPlanId",
                table: "Companies",
                column: "CurrentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_Name",
                table: "Plans",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Plans_CurrentPlanId",
                table: "Companies",
                column: "CurrentPlanId",
                principalTable: "Plans",
                principalColumn: "PlanId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Plans_CurrentPlanId",
                table: "Companies");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CurrentPlanId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CurrentPlanId",
                table: "Companies");
        }
    }
}
