using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLMPay.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMockRechargeFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MockRechargeOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MockAccountId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MockCheckoutSessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MockSubscriptionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockRechargeOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockRechargeOrders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MockRechargeOrders_OrderNumber",
                table: "MockRechargeOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MockRechargeOrders_UserId_CreatedAt",
                table: "MockRechargeOrders",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MockRechargeOrders");
        }
    }
}
