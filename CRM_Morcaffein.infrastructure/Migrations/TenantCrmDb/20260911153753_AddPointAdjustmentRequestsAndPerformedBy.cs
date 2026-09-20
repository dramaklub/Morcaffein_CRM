using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM_Morcaffein.infrastructure.Migrations.TenantCrmDb
{
    /// <inheritdoc />
    public partial class AddPointAdjustmentRequestsAndPerformedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PerformedBy",
                table: "PointLedgers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PointAdjustmentRequests",
                columns: table => new
                {
                    PointAdjustmentRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    PointsChange = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    RequestedBy = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionNotes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointAdjustmentRequests", x => x.PointAdjustmentRequestId);
                    table.ForeignKey(
                        name: "FK_PointAdjustmentRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PointAdjustmentRequests_Employees_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PointAdjustmentRequests_Employees_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PointLedgers_PerformedBy",
                table: "PointLedgers",
                column: "PerformedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PointAdjustmentRequests_ApprovedBy",
                table: "PointAdjustmentRequests",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PointAdjustmentRequests_CustomerId",
                table: "PointAdjustmentRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PointAdjustmentRequests_RequestedBy",
                table: "PointAdjustmentRequests",
                column: "RequestedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_PointLedgers_Employees_PerformedBy",
                table: "PointLedgers",
                column: "PerformedBy",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PointLedgers_Employees_PerformedBy",
                table: "PointLedgers");

            migrationBuilder.DropTable(
                name: "PointAdjustmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_PointLedgers_PerformedBy",
                table: "PointLedgers");

            migrationBuilder.DropColumn(
                name: "PerformedBy",
                table: "PointLedgers");
        }
    }
}
