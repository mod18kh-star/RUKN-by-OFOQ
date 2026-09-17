using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnManagementFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commerce_return_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    merchant_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_return_requests", x => x.id);
                    table.UniqueConstraint("ak_commerce_return_requests_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_commerce_return_requests_commerce_orders_tenant_id_order_id",
                        columns: x => new { x.tenant_id, x.order_id },
                        principalTable: "commerce_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commerce_return_requests_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commerce_return_request_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    restocked_quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    restocked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_return_request_items", x => x.id);
                    table.CheckConstraint("ck_return_items_quantity", "quantity > 0 AND restocked_quantity >= 0 AND restocked_quantity <= quantity");
                    table.ForeignKey(
                        name: "FK_commerce_return_request_items_commerce_return_requests_tena~",
                        columns: x => new { x.tenant_id, x.return_request_id },
                        principalTable: "commerce_return_requests",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_commerce_return_request_items_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_return_items_order_item",
                table: "commerce_return_request_items",
                columns: new[] { "tenant_id", "order_item_id" });

            migrationBuilder.CreateIndex(
                name: "ux_return_items_request_order_item",
                table: "commerce_return_request_items",
                columns: new[] { "tenant_id", "return_request_id", "order_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_tenant_customer_created",
                table: "commerce_return_requests",
                columns: new[] { "tenant_id", "customer_user_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_tenant_order",
                table: "commerce_return_requests",
                columns: new[] { "tenant_id", "order_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commerce_return_request_items");

            migrationBuilder.DropTable(
                name: "commerce_return_requests");
        }
    }
}
