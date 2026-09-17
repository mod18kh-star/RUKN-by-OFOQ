using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountCouponFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commerce_discount_coupons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    include_descendant_categories = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_order_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    maximum_total_uses = table.Column<int>(type: "integer", nullable: true),
                    maximum_uses_per_customer = table.Column<int>(type: "integer", nullable: true),
                    starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_discount_coupons", x => x.id);
                    table.UniqueConstraint("AK_commerce_discount_coupons_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_commerce_discount_coupons_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commerce_discount_coupon_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coupon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_discount_coupon_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_commerce_discount_coupon_categories_commerce_discount_coupo~",
                        columns: x => new { x.tenant_id, x.coupon_id },
                        principalTable: "commerce_discount_coupons",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commerce_discount_coupon_products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coupon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_discount_coupon_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_commerce_discount_coupon_products_commerce_discount_coupons~",
                        columns: x => new { x.tenant_id, x.coupon_id },
                        principalTable: "commerce_discount_coupons",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commerce_discount_coupon_categories_tenant_id_coupon_id_cat~",
                table: "commerce_discount_coupon_categories",
                columns: new[] { "tenant_id", "coupon_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commerce_discount_coupon_products_tenant_id_coupon_id_produ~",
                table: "commerce_discount_coupon_products",
                columns: new[] { "tenant_id", "coupon_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_discount_coupons_tenant_code",
                table: "commerce_discount_coupons",
                columns: new[] { "tenant_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commerce_discount_coupon_categories");

            migrationBuilder.DropTable(
                name: "commerce_discount_coupon_products");

            migrationBuilder.DropTable(
                name: "commerce_discount_coupons");
        }
    }
}
