using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutPricingAndCouponRedemptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "applied_coupon_code",
                table: "commerce_orders",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "commerce_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "shipping_address_id",
                table: "commerce_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_address_line1",
                table: "commerce_orders",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_address_line2",
                table: "commerce_orders",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_amount",
                table: "commerce_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "shipping_city",
                table: "commerce_orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_country_code",
                table: "commerce_orders",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "shipping_method_id",
                table: "commerce_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_method_name",
                table: "commerce_orders",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_method_type",
                table: "commerce_orders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_postal_code",
                table: "commerce_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_recipient_name",
                table: "commerce_orders",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_recipient_phone",
                table: "commerce_orders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_region",
                table: "commerce_orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "commerce_coupon_redemptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coupon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    redeemed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_coupon_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_commerce_coupon_redemptions_commerce_discount_coupons_tenan~",
                        columns: x => new { x.tenant_id, x.coupon_id },
                        principalTable: "commerce_discount_coupons",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_coupon_redemptions_coupon_customer",
                table: "commerce_coupon_redemptions",
                columns: new[] { "tenant_id", "coupon_id", "customer_user_id" });

            migrationBuilder.CreateIndex(
                name: "ux_coupon_redemptions_tenant_order",
                table: "commerce_coupon_redemptions",
                columns: new[] { "tenant_id", "order_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commerce_coupon_redemptions");

            migrationBuilder.DropColumn(
                name: "applied_coupon_code",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipping_address_id",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipping_address_line1",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipping_address_line2",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipping_amount",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipping_city",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipping_country_code",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipping_method_id",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipping_method_name",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipping_method_type",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipping_postal_code",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipping_recipient_name",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipping_recipient_phone",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipping_region",
                table: "commerce_orders");
        }
    }
}
