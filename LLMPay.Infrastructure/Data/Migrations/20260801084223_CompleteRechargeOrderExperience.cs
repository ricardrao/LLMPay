using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLMPay.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompleteRechargeOrderExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentProvider",
                table: "MockRechargeOrders",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "wechat");

            migrationBuilder.AddColumn<string>(
                name: "TargetName",
                table: "MockRechargeOrders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "待确认");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentProvider",
                table: "MockRechargeOrders");

            migrationBuilder.DropColumn(
                name: "TargetName",
                table: "MockRechargeOrders");
        }
    }
}
