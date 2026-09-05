using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommerceOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commerce_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    customer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_cart_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_orders", x => x.id);
                    table.UniqueConstraint("ak_commerce_orders_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_commerce_orders_source_cart",
                        columns: x => new { x.tenant_id, x.source_cart_id },
                        principalTable: "commerce_carts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commerce_orders_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commerce_order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    variant_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price_amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    unit_price_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_order_items", x => x.id);
                    table.CheckConstraint("ck_commerce_order_items_price_nonnegative", "unit_price_amount >= 0");
                    table.CheckConstraint("ck_commerce_order_items_quantity", "quantity > 0 AND quantity <= 999");
                    table.ForeignKey(
                        name: "fk_commerce_order_items_orders",
                        columns: x => new { x.tenant_id, x.order_id },
                        principalTable: "commerce_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_commerce_order_items_products",
                        columns: x => new { x.tenant_id, x.product_id },
                        principalTable: "catalog_products",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commerce_order_items_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commerce_order_items_variants",
                        columns: x => new { x.tenant_id, x.product_id, x.product_variant_id },
                        principalTable: "catalog_product_variants",
                        principalColumns: new[] { "tenant_id", "product_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_order_items_order",
                table: "commerce_order_items",
                columns: new[] { "tenant_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_order_items_product_variant",
                table: "commerce_order_items",
                columns: new[] { "tenant_id", "product_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "ux_commerce_order_items_order_variant",
                table: "commerce_order_items",
                columns: new[] { "tenant_id", "order_id", "product_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commerce_orders_tenant_customer_created",
                table: "commerce_orders",
                columns: new[] { "tenant_id", "customer_user_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_orders_tenant_status",
                table: "commerce_orders",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_commerce_orders_tenant_source_cart",
                table: "commerce_orders",
                columns: new[] { "tenant_id", "source_cart_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commerce_order_items");

            migrationBuilder.DropTable(
                name: "commerce_orders");
        }
    }
}
