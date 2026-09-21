using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualOrderPaymentProofs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commerce_manual_order_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    account_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    account_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    protected_account_snapshot = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_manual_order_payments", x => x.id);
                    table.UniqueConstraint("AK_commerce_manual_order_payments_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_commerce_manual_order_payments_commerce_orders_tenant_id_or~",
                        columns: x => new { x.tenant_id, x.order_id },
                        principalTable: "commerce_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commerce_manual_payment_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manual_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ciphertext = table.Column<byte[]>(type: "bytea", nullable: false),
                    nonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    auth_tag = table.Column<byte[]>(type: "bytea", nullable: false),
                    transfer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    review_status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_manual_payment_receipts", x => x.id);
                    table.ForeignKey(
                        name: "FK_commerce_manual_payment_receipts_commerce_manual_order_paym~",
                        columns: x => new { x.tenant_id, x.manual_payment_id },
                        principalTable: "commerce_manual_order_payments",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commerce_manual_order_payments_tenant_id_status_updated_at_~",
                table: "commerce_manual_order_payments",
                columns: new[] { "tenant_id", "status", "updated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_manual_order_payment_tenant_order",
                table: "commerce_manual_order_payments",
                columns: new[] { "tenant_id", "order_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commerce_manual_payment_receipts_tenant_id_manual_payment_i~",
                table: "commerce_manual_payment_receipts",
                columns: new[] { "tenant_id", "manual_payment_id", "submitted_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commerce_manual_payment_receipts");

            migrationBuilder.DropTable(
                name: "commerce_manual_order_payments");
        }
    }
}
