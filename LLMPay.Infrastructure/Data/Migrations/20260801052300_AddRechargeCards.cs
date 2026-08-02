using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLMPay.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRechargeCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RechargeCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CodeSuffix = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Credits = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BatchName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RedeemedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RedeemedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RechargeCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RechargeCards_Users_RedeemedByUserId",
                        column: x => x.RedeemedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RechargeCards_CodeHash",
                table: "RechargeCards",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RechargeCards_RedeemedByUserId",
                table: "RechargeCards",
                column: "RedeemedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RechargeCards_Status_CreatedAt",
                table: "RechargeCards",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RechargeCards");
        }
    }
}
