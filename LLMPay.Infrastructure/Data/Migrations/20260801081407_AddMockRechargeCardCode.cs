using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLMPay.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMockRechargeCardCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RechargeCodeHash",
                table: "MockRechargeOrders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RechargeCodeProtected",
                table: "MockRechargeOrders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RechargeCodeSuffix",
                table: "MockRechargeOrders",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RechargeCodeHash",
                table: "MockRechargeOrders");

            migrationBuilder.DropColumn(
                name: "RechargeCodeProtected",
                table: "MockRechargeOrders");

            migrationBuilder.DropColumn(
                name: "RechargeCodeSuffix",
                table: "MockRechargeOrders");
        }
    }
}
