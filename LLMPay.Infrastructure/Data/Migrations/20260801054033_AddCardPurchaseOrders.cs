using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLMPay.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCardPurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardPurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Credits = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProviderTradeNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IssuedCardCodeProtected = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IssuedCardId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardPurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardPurchaseOrders_RechargeCards_IssuedCardId",
                        column: x => x.IssuedCardId,
                        principalTable: "RechargeCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardPurchaseOrders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardPurchaseOrders_IssuedCardId",
                table: "CardPurchaseOrders",
                column: "IssuedCardId");

            migrationBuilder.CreateIndex(
                name: "IX_CardPurchaseOrders_OrderNumber",
                table: "CardPurchaseOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardPurchaseOrders_UserId_CreatedAt",
                table: "CardPurchaseOrders",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardPurchaseOrders");
        }
    }
}
