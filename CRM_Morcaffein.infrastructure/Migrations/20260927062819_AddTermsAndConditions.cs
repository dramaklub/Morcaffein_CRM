using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM_Morcaffein.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTermsAndConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TermsAndConditions",
                columns: table => new
                {
                    TermsId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TermsAndConditions", x => x.TermsId);
                    table.ForeignKey(
                        name: "FK_TermsAndConditions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Restrict);
                });
            // Seed placeholder T&C documents, one per plan. The real body text
            // gets swapped in later via SQL or (eventually) an edit UI — this
            // just ensures the table has the three expected rows so the client
            // can render cards and show content.
            //
            // The Body text is deliberately short and clearly marked as a
            // placeholder — do NOT ship this as-is. Replace via SSMS:
            //   UPDATE TermsAndConditions SET Body = N'...real text...'
            //   WHERE PlanId = 1;
            migrationBuilder.InsertData(
                table: "TermsAndConditions",
                columns: new[] { "PlanId", "Title", "Body", "Version", "EffectiveFrom", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    {
                        1,
                        "Basic Plan — Terms & Conditions",
                        "PLACEHOLDER — replace with the real Basic plan terms via SSMS.",
                        "1.0",
                        new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                        DateTime.UtcNow,
                        DateTime.UtcNow
                    },
                    {
                        2,
                        "Standard Plan — Terms & Conditions",
                        "PLACEHOLDER — replace with the real Standard plan terms via SSMS.",
                        "1.0",
                        new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                        DateTime.UtcNow,
                        DateTime.UtcNow
                    },
                    {
                        3,
                        "Full Plan — Terms & Conditions",
                        "PLACEHOLDER — replace with the real Full plan terms via SSMS.",
                        "1.0",
                        new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                        DateTime.UtcNow,
                        DateTime.UtcNow
                    },
                });

            migrationBuilder.CreateIndex(
                name: "IX_TermsAndConditions_PlanId",
                table: "TermsAndConditions",
                column: "PlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TermsAndConditions");
        }
    }
}
