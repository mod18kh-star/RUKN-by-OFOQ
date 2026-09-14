using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderInventoryMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_inventory_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    quantity_before = table.Column<int>(type: "integer", nullable: false),
                    quantity_after = table.Column<int>(type: "integer", nullable: false),
                    quantity_delta = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_inventory_movements", x => x.id);
                    table.CheckConstraint("ck_catalog_inventory_movements_after_nonnegative", "quantity_after >= 0");
                    table.CheckConstraint("ck_catalog_inventory_movements_before_nonnegative", "quantity_before >= 0");
                    table.CheckConstraint("ck_catalog_inventory_movements_delta_consistent", "quantity_after - quantity_before = quantity_delta");
                    table.CheckConstraint("ck_catalog_inventory_movements_delta_nonzero", "quantity_delta <> 0");
                    table.ForeignKey(
                        name: "fk_catalog_inventory_movements_orders",
                        columns: x => new { x.tenant_id, x.order_id },
                        principalTable: "commerce_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_inventory_movements_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_inventory_movements_variants",
                        columns: x => new { x.tenant_id, x.product_id, x.product_variant_id },
                        principalTable: "catalog_product_variants",
                        principalColumns: new[] { "tenant_id", "product_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_inventory_movements_order_created",
                table: "catalog_inventory_movements",
                columns: new[] { "tenant_id", "order_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_inventory_movements_tenant_id_product_id_product_va~",
                table: "catalog_inventory_movements",
                columns: new[] { "tenant_id", "product_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "ux_catalog_inventory_movements_order_variant_type",
                table: "catalog_inventory_movements",
                columns: new[] { "tenant_id", "order_id", "product_variant_id", "type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_inventory_movements");
        }
    }
}
