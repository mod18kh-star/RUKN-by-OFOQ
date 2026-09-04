using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommerceCarts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commerce_carts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    customer_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_carts", x => x.id);
                    table.UniqueConstraint("ak_commerce_carts_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_commerce_carts_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commerce_cart_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cart_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_price_amount = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                    unit_price_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_cart_items", x => x.id);
                    table.CheckConstraint("ck_commerce_cart_items_price_nonnegative", "unit_price_amount >= 0");
                    table.CheckConstraint("ck_commerce_cart_items_quantity", "quantity > 0 AND quantity <= 999");
                    table.ForeignKey(
                        name: "fk_commerce_cart_items_carts",
                        columns: x => new { x.tenant_id, x.cart_id },
                        principalTable: "commerce_carts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_commerce_cart_items_products",
                        columns: x => new { x.tenant_id, x.product_id },
                        principalTable: "catalog_products",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commerce_cart_items_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commerce_cart_items_variants",
                        columns: x => new { x.tenant_id, x.product_id, x.product_variant_id },
                        principalTable: "catalog_product_variants",
                        principalColumns: new[] { "tenant_id", "product_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_cart_items_cart",
                table: "commerce_cart_items",
                columns: new[] { "tenant_id", "cart_id" });

            migrationBuilder.CreateIndex(
                name: "IX_commerce_cart_items_tenant_id_product_id_product_variant_id",
                table: "commerce_cart_items",
                columns: new[] { "tenant_id", "product_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "ux_commerce_cart_items_cart_variant",
                table: "commerce_cart_items",
                columns: new[] { "tenant_id", "cart_id", "product_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commerce_carts_tenant_status",
                table: "commerce_carts",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_commerce_carts_active_customer",
                table: "commerce_carts",
                columns: new[] { "tenant_id", "customer_user_id" },
                unique: true,
                filter: "status = 0 AND customer_user_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commerce_cart_items");

            migrationBuilder.DropTable(
                name: "commerce_carts");
        }
    }
}
