using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OFOQ.Market.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentProviderAccountsAndWallets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commerce_tenant_payment_provider_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    environment = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    protected_credentials = table.Column<string>(type: "text", nullable: true),
                    credentials_version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_tenant_payment_provider_accounts", x => x.id);
                    table.UniqueConstraint("ak_commerce_tenant_payment_provider_accounts_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "fk_commerce_tenant_payment_provider_accounts_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commerce_tenant_payment_wallet_capabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_type = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_tenant_payment_wallet_capabilities", x => x.id);
                    table.ForeignKey(
                        name: "fk_commerce_pay_wallet_caps_provider_accounts",
                        columns: x => new { x.tenant_id, x.provider_account_id },
                        principalTable: "commerce_tenant_payment_provider_accounts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_commerce_tenant_payment_wallet_capabilities_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_commerce_pay_provider_accounts_tenant_provider_enabled",
                table: "commerce_tenant_payment_provider_accounts",
                columns: new[] { "tenant_id", "provider_code" },
                unique: true,
                filter: "is_enabled = TRUE");

            migrationBuilder.CreateIndex(
                name: "ux_commerce_pay_provider_accounts_tenant_provider_env",
                table: "commerce_tenant_payment_provider_accounts",
                columns: new[] { "tenant_id", "provider_code", "environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_commerce_pay_wallet_caps_tenant_account_wallet",
                table: "commerce_tenant_payment_wallet_capabilities",
                columns: new[] { "tenant_id", "provider_account_id", "wallet_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commerce_tenant_payment_wallet_capabilities");

            migrationBuilder.DropTable(
                name: "commerce_tenant_payment_provider_accounts");
        }
    }
}
