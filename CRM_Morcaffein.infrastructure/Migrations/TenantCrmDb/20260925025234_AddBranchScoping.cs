using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM_Morcaffein.infrastructure.Migrations.TenantCrmDb
{
    /// <inheritdoc />
    public partial class AddBranchScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- 1. Create the Branches table ----
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    BranchId = table.Column<int>(nullable: false)
                                      .Annotation("SqlServer:Identity", "1, 1"),
                    BranchName = table.Column<string>(maxLength: 100, nullable: false),
                    Address = table.Column<string>(maxLength: 250, nullable: false),
                    IsActive = table.Column<bool>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.BranchId);
                });

            // ---- 2. Seed two branches ----
            // Identity insert on so we control the IDs and can reference them
            // in the backfill below.
            migrationBuilder.InsertData(
                table: "Branches",
                columns: new[] { "BranchId", "BranchName", "Address", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Branch 1", "", true, DateTime.UtcNow, DateTime.UtcNow },
                    { 2, "Branch 2", "", true, DateTime.UtcNow, DateTime.UtcNow },
                });

            // ---- 3. Add nullable BranchId to each affected table ----
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Orders",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Campaigns",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Feedbacks",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Employees",
                nullable: true);

            // ---- 4. Backfill existing rows to Branch 1 ----
            migrationBuilder.Sql("UPDATE Orders    SET BranchId = 1 WHERE BranchId IS NULL;");
            migrationBuilder.Sql("UPDATE Campaigns SET BranchId = 1 WHERE BranchId IS NULL;");
            migrationBuilder.Sql("UPDATE Feedbacks SET BranchId = 1 WHERE BranchId IS NULL;");
            migrationBuilder.Sql("UPDATE Employees SET BranchId = 1 WHERE BranchId IS NULL;");

            // ---- 5. Make the FK non-nullable on Orders / Campaigns / Feedbacks ----
            // Employees stays nullable — Admins have no branch.
            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "Orders",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "Campaigns",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "Feedbacks",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            // ---- 6. Foreign keys and indexes ----
            migrationBuilder.CreateIndex(
                name: "IX_Orders_BranchId",
                table: "Orders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_BranchId",
                table: "Campaigns",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_BranchId",
                table: "Feedbacks",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_BranchId",
                table: "Employees",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Branches_BranchId",
                table: "Orders",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Branches_BranchId",
                table: "Campaigns",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Branches_BranchId",
                table: "Feedbacks",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Branches_BranchId",
                table: "Employees",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_Orders_Branches_BranchId", "Orders");
            migrationBuilder.DropForeignKey("FK_Campaigns_Branches_BranchId", "Campaigns");
            migrationBuilder.DropForeignKey("FK_Feedbacks_Branches_BranchId", "Feedbacks");
            migrationBuilder.DropForeignKey("FK_Employees_Branches_BranchId", "Employees");

            migrationBuilder.DropIndex("IX_Orders_BranchId", "Orders");
            migrationBuilder.DropIndex("IX_Campaigns_BranchId", "Campaigns");
            migrationBuilder.DropIndex("IX_Feedbacks_BranchId", "Feedbacks");
            migrationBuilder.DropIndex("IX_Employees_BranchId", "Employees");

            migrationBuilder.DropColumn("BranchId", "Orders");
            migrationBuilder.DropColumn("BranchId", "Campaigns");
            migrationBuilder.DropColumn("BranchId", "Feedbacks");
            migrationBuilder.DropColumn("BranchId", "Employees");

            migrationBuilder.DropTable("Branches");
        }   
    }
}
