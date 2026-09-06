using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commerce_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    customer_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_payments", x => x.id);
                    table.UniqueConstraint("ak_commerce_payments_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_commerce_payments_amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "fk_commerce_payments_orders",
                        columns: x => new { x.tenant_id, x.order_id },
                        principalTable: "commerce_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commerce_payments_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commerce_tenant_payment_capabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    electronic_payments_status = table.Column<int>(type: "integer", nullable: false),
                    suspension_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    suspended_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_tenant_payment_capabilities", x => x.id);
                    table.ForeignKey(
                        name: "fk_commerce_tenant_payment_capabilities_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commerce_tenant_payment_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method_type = table.Column<int>(type: "integer", nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    minimum_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    maximum_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    provider_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_tenant_payment_methods", x => x.id);
                    table.UniqueConstraint("ak_commerce_tenant_payment_methods_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_commerce_tenant_payment_methods_limits_order", "minimum_amount IS NULL OR maximum_amount IS NULL OR minimum_amount <= maximum_amount");
                    table.CheckConstraint("ck_commerce_tenant_payment_methods_maximum_nonnegative", "maximum_amount IS NULL OR maximum_amount >= 0");
                    table.CheckConstraint("ck_commerce_tenant_payment_methods_minimum_nonnegative", "minimum_amount IS NULL OR minimum_amount >= 0");
                    table.ForeignKey(
                        name: "fk_commerce_tenant_payment_methods_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commerce_payment_intents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_payment_method_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    method_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    create_idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    customer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_payment_intents", x => x.id);
                    table.UniqueConstraint("ak_commerce_payment_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_commerce_payment_intents_amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "fk_commerce_payment_intents_payments",
                        columns: x => new { x.tenant_id, x.payment_id },
                        principalTable: "commerce_payments",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_commerce_payment_intents_tenant_payment_methods",
                        columns: x => new { x.tenant_id, x.tenant_payment_method_id },
                        principalTable: "commerce_tenant_payment_methods",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commerce_payment_intents_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commerce_payment_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    resulting_status = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    external_event_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_payment_transactions", x => x.id);
                    table.CheckConstraint("ck_commerce_payment_transactions_amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "fk_commerce_payment_transactions_intents",
                        columns: x => new { x.tenant_id, x.payment_intent_id },
                        principalTable: "commerce_payment_intents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_payment_intents_payment_created",
                table: "commerce_payment_intents",
                columns: new[] { "tenant_id", "payment_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_payment_intents_tenant_status",
                table: "commerce_payment_intents",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_commerce_payment_intents_create_idempotency",
                table: "commerce_payment_intents",
                columns: new[] { "tenant_id", "customer_user_id", "create_idempotency_key" },
                unique: true,
                filter: "create_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_commerce_payment_intents_method_provider_reference",
                table: "commerce_payment_intents",
                columns: new[] { "tenant_id", "tenant_payment_method_id", "provider_reference" },
                unique: true,
                filter: "provider_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_commerce_payment_transactions_intent_created",
                table: "commerce_payment_transactions",
                columns: new[] { "tenant_id", "payment_intent_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_commerce_payment_transactions_external_event",
                table: "commerce_payment_transactions",
                columns: new[] { "tenant_id", "payment_intent_id", "external_event_id" },
                unique: true,
                filter: "external_event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_commerce_payments_tenant_customer_created",
                table: "commerce_payments",
                columns: new[] { "tenant_id", "customer_user_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_payments_tenant_status",
                table: "commerce_payments",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_commerce_payments_tenant_order",
                table: "commerce_payments",
                columns: new[] { "tenant_id", "order_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_commerce_tenant_payment_capabilities_tenant",
                table: "commerce_tenant_payment_capabilities",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commerce_tenant_payment_methods_provider_currency",
                table: "commerce_tenant_payment_methods",
                columns: new[] { "tenant_id", "provider_code", "currency_code" });

            migrationBuilder.CreateIndex(
                name: "ix_commerce_tenant_payment_methods_tenant_enabled",
                table: "commerce_tenant_payment_methods",
                columns: new[] { "tenant_id", "is_enabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commerce_payment_transactions");

            migrationBuilder.DropTable(
                name: "commerce_tenant_payment_capabilities");

            migrationBuilder.DropTable(
                name: "commerce_payment_intents");

            migrationBuilder.DropTable(
                name: "commerce_payments");

            migrationBuilder.DropTable(
                name: "commerce_tenant_payment_methods");
        }
    }
}
