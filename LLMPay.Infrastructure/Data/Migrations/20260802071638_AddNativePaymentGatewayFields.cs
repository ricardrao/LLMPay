using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLMPay.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNativePaymentGatewayFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaymentAmountCny",
                table: "MockRechargeOrders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentCodeUrl",
                table: "MockRechargeOrders",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentExpiresAt",
                table: "MockRechargeOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentGatewayTradeNo",
                table: "MockRechargeOrders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMode",
                table: "MockRechargeOrders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "local");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentAmountCny",
                table: "MockRechargeOrders");

            migrationBuilder.DropColumn(
                name: "PaymentCodeUrl",
                table: "MockRechargeOrders");

            migrationBuilder.DropColumn(
                name: "PaymentExpiresAt",
                table: "MockRechargeOrders");

            migrationBuilder.DropColumn(
                name: "PaymentGatewayTradeNo",
                table: "MockRechargeOrders");

            migrationBuilder.DropColumn(
                name: "PaymentMode",
                table: "MockRechargeOrders");
        }
    }
}
