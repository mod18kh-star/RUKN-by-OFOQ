using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderLifecycleFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "commerce_orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at_utc",
                table: "commerce_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "delivered_at_utc",
                table: "commerce_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "fulfillment_status",
                table: "commerce_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "shipped_at_utc",
                table: "commerce_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_carrier",
                table: "commerce_orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tracking_number",
                table: "commerce_orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "commerce_order_timeline",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    order_status = table.Column<int>(type: "integer", nullable: false),
                    fulfillment_status = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_order_timeline", x => x.id);
                    table.ForeignKey(
                        name: "fk_commerce_order_timeline_orders",
                        columns: x => new { x.tenant_id, x.order_id },
                        principalTable: "commerce_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_commerce_order_timeline_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_order_timeline_order_created",
                table: "commerce_order_timeline",
                columns: new[] { "tenant_id", "order_id", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commerce_order_timeline");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "cancelled_at_utc",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "delivered_at_utc",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "fulfillment_status",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipped_at_utc",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "shipping_carrier",
                table: "commerce_orders");

            migrationBuilder.DropColumn(
                name: "tracking_number",
                table: "commerce_orders");
        }
    }
}
